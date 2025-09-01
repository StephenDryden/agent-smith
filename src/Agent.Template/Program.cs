using Agent.Core;
using Agent.Mcp.Clients;
using Agent.Mcp.Protocol;
using Agent.Providers;
using System.Net;
using System.Text.Json;
using Agent.Template;

var builder = WebApplication.CreateBuilder(args);

// Load baked config if present
AgentConfig? agentConfig = null;
var configPath = Environment.GetEnvironmentVariable("AGENT_CONFIG_PATH") ?? "/app/config/agent.json";
if (File.Exists(configPath))
{
    var json = await File.ReadAllTextAsync(configPath);
    // simple env substitution for ${VAR}
    json = EnvSubstitute(json);
    agentConfig = JsonSerializer.Deserialize<AgentConfig>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });
}

// Bind to config port (or default 8080)
var desiredPort = agentConfig?.Runtime?.Port is int p && p > 0 ? p : 8080;
builder.WebHost.UseUrls($"http://0.0.0.0:{desiredPort}");

var app = builder.Build();

// Root placeholder
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "agent-smith" }));

// Health endpoint
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

// MCP tooling diagnostics: list available tools via MCP
app.MapGet("/mcp/tools", async () =>
{
    if (agentConfig is null) return Results.BadRequest(new { error = "no agent config loaded" });
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using var mcp = new McpClient(agentConfig);
        await mcp.InitializeAsync(cts.Token);

        var id = Guid.NewGuid().ToString("n");
        var req = new JsonRpcRequest { Id = id, Method = "tools/list", Params = new { } };
        await mcp.SendAsync(new[] { req }, cts.Token);

        await foreach (var msg in mcp.ReceiveAsync(cts.Token))
        {
            if (msg is JsonRpcResponse resp && resp.Id == id)
            {
                return Results.Ok(resp.Result);
            }
        }
        return Results.Problem("No response from MCP tools/list", statusCode: 504);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 502);
    }
});

// MCP tooling diagnostics: call a tool by name
app.MapPost("/mcp/call", async (HttpContext ctx) =>
{
    if (agentConfig is null) return Results.BadRequest(new { error = "no agent config loaded" });
    try
    {
        using var ctsRead = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync(ctsRead.Token);
        var call = JsonSerializer.Deserialize<McpCallRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (call is null || string.IsNullOrWhiteSpace(call.Name))
            return Results.BadRequest(new { error = "missing name" });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var mcp = new McpClient(agentConfig);
        await mcp.InitializeAsync(cts.Token);
        var id = Guid.NewGuid().ToString("n");
        var req = new JsonRpcRequest { Id = id, Method = "tools/call", Params = new { name = call.Name, arguments = call.Arguments ?? new { } } };
        await mcp.SendAsync(new[] { req }, cts.Token);

        await foreach (var msg in mcp.ReceiveAsync(cts.Token))
        {
            if (msg is JsonRpcResponse resp && resp.Id == id)
            {
                return Results.Ok(resp.Result ?? new { });
            }
        }
        return Results.Problem("No response from MCP tools/call", statusCode: 504);
    }
    catch (OperationCanceledException)
    {
        return Results.Problem("Request timed out", statusCode: 504);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 502);
    }
});

// Chat endpoint (non-streaming v1)
app.MapPost("/v1/chat", async (HttpContext ctx) =>
{
    // Read body quickly to avoid long hangs
    ChatRequest? req = null;
    try
    {
        using var ctsRead = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync(ctsRead.Token);
        if (string.IsNullOrWhiteSpace(body))
            return Results.BadRequest(new { error = "empty body" });
        req = JsonSerializer.Deserialize<ChatRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (req is null) return Results.BadRequest(new { error = "invalid json" });
    }
    catch (OperationCanceledException)
    {
        return Results.BadRequest(new { error = "read timeout" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    // Prepare messages with persona/system prompt first
    var sys = (agentConfig?.Agent.SystemPrompt?.Trim()) ?? "You are helpful.";
    var messages = new List<ChatMessage>();
    if (!string.IsNullOrWhiteSpace(sys))
        messages.Add(new ChatMessage("system", sys));
    if (req?.Messages is { Count: > 0 })
        messages.AddRange(req.Messages);

    // Initialize MCP session (best-effort) unless explicitly skipped via env
    var skipMcp = string.Equals(Environment.GetEnvironmentVariable("SKIP_MCP_INIT"), "true", StringComparison.OrdinalIgnoreCase);
    if (!skipMcp)
    {
        try
        {
            if (agentConfig is not null)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                await using var mcp = new McpClient(agentConfig);
                await mcp.InitializeAsync(cts.Token);
                var init = new JsonRpcRequest
                {
                    Id = Guid.NewGuid().ToString("n"),
                    Method = "initialize",
                    Params = new { protocolVersion = "2024-11-05", capabilities = new { } }
                };
                await mcp.SendAsync(new[] { init }, cts.Token);
                var count = 0;
                var drainCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                drainCts.CancelAfter(TimeSpan.FromSeconds(5));
                await foreach (var _ in mcp.ReceiveAsync(drainCts.Token))
                {
                    if (++count >= 3) break;
                }
            }
        }
        catch { /* ignore MCP errors for now */ }
    }

    // Call model provider (GitHub Models by default; supports MOCK via config or env)
    try
    {
        var providerName = (agentConfig?.Model.Provider ?? "github-models").ToLowerInvariant();
        var modelId = agentConfig?.Model.ModelId ?? "openai/gpt-4o-mini";
        var temperature = agentConfig?.Model.Parameters.Temperature ?? 0.2;
        var topP = agentConfig?.Model.Parameters.TopP ?? 1.0;

        // Enable override via env for quick testing
        var providerOverride = Environment.GetEnvironmentVariable("MODEL_PROVIDER");
        if (!string.IsNullOrWhiteSpace(providerOverride)) providerName = providerOverride.ToLowerInvariant();

        string content;
        if (providerName is "mock")
        {
            var mock = new MockModelsProvider();
            content = await mock.GetChatCompletionAsync(modelId, messages, temperature, topP, ctx.RequestAborted);
        }
        else
        {
            var token = Environment.GetEnvironmentVariable("GITHUB_MODELS_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.Problem("GITHUB_MODELS_TOKEN is not set", statusCode: (int)HttpStatusCode.InternalServerError);
            }
            using var provider = new GitHubModelsProvider(token);
            content = await provider.GetChatCompletionAsync(modelId, messages, temperature, topP, ctx.RequestAborted);
        }
        return Results.Ok(new ChatResponse { Message = new ChatMessage("assistant", content) });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: (int)HttpStatusCode.BadGateway);
    }
});

app.Run();

static string EnvSubstitute(string input)
{
    return System.Text.RegularExpressions.Regex.Replace(input, @"\$\{([A-Z0-9_]+)\}", m =>
    {
        var key = m.Groups[1].Value;
        var val = Environment.GetEnvironmentVariable(key);
        return val ?? m.Value;
    });
}

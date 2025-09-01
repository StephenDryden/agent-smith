using Agent.Core;
using System.Text.Json;

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

var app = builder.Build();

// Root placeholder
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "agent-smith" }));

// Health endpoint
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

// Chat endpoint (non-streaming v1)
app.MapPost("/v1/chat", (ChatRequest req) =>
{
    var persona = agentConfig?.Agent.SystemPrompt ?? "You are helpful.";
    var lastUser = req.Messages.LastOrDefault(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase))?.Content ?? string.Empty;
    var content = $"[persona]\n{persona}\n\n[echo]\n{lastUser}";
    return Results.Ok(new ChatResponse { Message = new ChatMessage("assistant", content) });
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

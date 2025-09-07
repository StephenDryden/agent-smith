using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Load configuration with validation
var configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "commander.agent.json");
if (!File.Exists(configPath))
{
    Console.WriteLine("Configuration file not found: " + configPath);
    return;
}

var configJson = await File.ReadAllTextAsync(configPath);
var agents = new List<AgentInfo>();
try
{
    var agentElements = JsonDocument.Parse(configJson).RootElement.GetProperty("agents").EnumerateArray();
    foreach (var agentElement in agentElements)
    {
        var name = agentElement.GetProperty("name").GetString();
        var endpoint = agentElement.GetProperty("endpoint").GetString();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(endpoint))
        {
            Console.WriteLine("Skipping invalid agent configuration: " + agentElement);
            continue;
        }

        var capabilities = agentElement.GetProperty("capabilities")
            .EnumerateArray()
            .Select(c => c.GetString() ?? string.Empty)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();
        agents.Add(new AgentInfo { Name = name, Endpoint = endpoint, Capabilities = capabilities });
    }
}
catch (Exception ex)
{
    Console.WriteLine("Failed to parse configuration: " + ex.Message);
    return;
}

// Load prompt from configuration
string prompt;
try
{
    prompt = JsonDocument.Parse(configJson).RootElement.GetProperty("prompt").GetString() ?? "";
    if (string.IsNullOrWhiteSpace(prompt))
    {
        Console.WriteLine("Prompt is missing or empty in the configuration.");
        return;
    }
}
catch (Exception ex)
{
    Console.WriteLine("Failed to parse prompt from configuration: " + ex.Message);
    return;
}

// Add AgentRegistry
var agentRegistry = new AgentRegistry(new HttpClient(), agents);
builder.Services.AddSingleton(agentRegistry);

// Add RequestParser
builder.Services.AddSingleton(new RequestParser(agentRegistry));

// Add AgentCommunicator
builder.Services.AddSingleton(new AgentCommunicator(new HttpClient()));

var app = builder.Build();

// Initialize AgentRegistry
await agentRegistry.InitializeAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

// Add logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Log discovered agents and capabilities
logger.LogInformation("Discovered Agents and Capabilities:");
foreach (var agent in agents)
{
    logger.LogInformation($"Agent: {agent.Name}, Capabilities: {string.Join(", ", agent.Capabilities)}");
}

// Log the prompt
logger.LogInformation("Commander Agent Prompt: {Prompt}", prompt);

// Log discovered agents and their dynamically fetched tools
logger.LogInformation("Discovered Agents and Their Tools:");
foreach (var agent in agents)
{
    logger.LogInformation($"Agent: {agent.Name}, Tools: {string.Join(", ", agent.Capabilities)}");
}

// Update /healthz endpoint to include connected agents' health
app.MapGet("/healthz", async (AgentRegistry registry) =>
{
    var agentHealth = await registry.GetAgentHealthAsync();
    return Results.Json(new { status = "healthy", agents = agentHealth });
});

// Add endpoint to list tools available via connected agents
app.MapGet("/tools", async (AgentRegistry registry) =>
{
    try
    {
        Console.WriteLine("Received request for /tools endpoint.");
        var tools = await registry.GetAvailableToolsAsync();
        Console.WriteLine("Successfully fetched tools for all agents.");
        return Results.Ok(tools);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in /tools endpoint: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        return Results.Problem("An error occurred while fetching tools.");
    }
});

// Add endpoint to list connected agents and their tools

app.MapGet("/agents", (AgentRegistry registry) => Results.Ok(registry.GetAgents()));

// Add chat endpoint to forward user requests to the correct agent
app.MapPost("/v1/chat", async (HttpContext ctx, AgentRegistry registry, AgentCommunicator communicator) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();

    Console.WriteLine($"[CHAT] Incoming request body: {body}");

    string userInput = "";
    try
    {
        var doc = JsonDocument.Parse(body);
        var messages = doc.RootElement.TryGetProperty("messages", out var msgProp) ? msgProp : doc.RootElement.GetProperty("messages");
        if (messages.ValueKind == JsonValueKind.Array && messages.GetArrayLength() > 0)
        {
            var firstMsg = messages[0];
            userInput = firstMsg.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? "" : "";
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CHAT] Invalid request format: {ex.Message}");
        return Results.Problem($"Invalid request format: {ex.Message}", statusCode: 400);
    }

    Console.WriteLine($"[CHAT] Parsed user input: {userInput}");

    if (string.IsNullOrWhiteSpace(userInput))
    {
        Console.WriteLine("[CHAT] No user input found in request.");
        return Results.Problem("No user input found in request.", statusCode: 400);
    }

    // Use RequestParser to select agent(s)
    var parser = new RequestParser(registry);
    var agents = parser.ParseRequest(userInput);

    Console.WriteLine($"[CHAT] Matching agents: {string.Join(", ", agents.Select(a => a.Name))}");

    if (agents.Count == 0)
    {
        Console.WriteLine("[CHAT] No matching agent found.");
        return Results.Problem("No matching agent found.", statusCode: 404);
    }

    // For now, just use the first matching agent
    var agent = agents[0];
    Console.WriteLine($"[CHAT] Selected agent: {agent.Name}, endpoint: {agent.Endpoint}");

    // Forward the request to the agent
    var response = await communicator.SendRequestAsync(agent, userInput);

    if (response.Error != null)
    {
        Console.WriteLine($"[CHAT] Error from agent: {response.Error}");
        return Results.Problem(response.Error, statusCode: 502);
    }

    Console.WriteLine($"[CHAT] Agent response: {response.Response}");
    return Results.Ok(new { agent = agent.Name, response = response.Response });
});

app.Run();

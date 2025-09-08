using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Load configuration with validation
var configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "orchestrator.agent.json");
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
	// ...existing code...
}

using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Load configuration
var configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "commander.agent.json");
var configJson = await File.ReadAllTextAsync(configPath);
var agents = JsonSerializer.Deserialize<List<Agent>>(JsonDocument.Parse(configJson).RootElement.GetProperty("agents").ToString()) ?? new List<Agent>();

// Add AgentRegistry
var agentRegistry = new AgentRegistry(new HttpClient(), agents);
builder.Services.AddSingleton(agentRegistry);

// Add RequestParser
builder.Services.AddSingleton(new RequestParser(agentRegistry));

var app = builder.Build();

// Initialize AgentRegistry
await agentRegistry.InitializeAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.MapControllers();

app.Run();

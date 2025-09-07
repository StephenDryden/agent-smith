using System.Net.Http;
using System.Text.Json;

public class AgentRegistry
{
    private readonly HttpClient _httpClient;
    private readonly List<AgentInfo> _agents;

    public AgentRegistry(HttpClient httpClient, List<AgentInfo> agents)
    {
        _httpClient = httpClient;
        _agents = agents;
    }

    public async Task InitializeAsync()
    {
        foreach (var agent in _agents)
        {
            try
            {
                if (!Uri.TryCreate(agent.Endpoint, UriKind.Absolute, out var baseUri))
                {
                    Console.WriteLine($"Invalid endpoint for agent {agent.Name}: {agent.Endpoint}");
                    continue;
                }

                var toolsUri = new Uri(baseUri, "/mcp/tools"); // Ensure toolsUri is defined here

                int retryCount = 3;
                while (retryCount > 0)
                {
                    try
                    {
                        Console.WriteLine($"Attempting to fetch tools for agent '{agent.Name}' at {toolsUri}...");
                        var response = await _httpClient.GetAsync(toolsUri);
                        Console.WriteLine($"Response status code: {response.StatusCode}");

                        response.EnsureSuccessStatusCode();

                        // Deserialize into ToolResponse to handle the 'tools' wrapper
                        var toolResponse = await JsonSerializer.DeserializeAsync<ToolResponse>(
                            await response.Content.ReadAsStreamAsync());

                        var tools = toolResponse?.Tools;
                        agent.Capabilities = tools?.Select(t => t.Name).ToList() ?? new List<string>();
                        Console.WriteLine($"Tools for agent '{agent.Name}': {string.Join(", ", agent.Capabilities)}");
                        break;
                    }
                    catch (HttpRequestException httpEx)
                    {
                        retryCount--;
                        Console.WriteLine($"HTTP request failed for agent '{agent.Name}'. Retries left: {retryCount}. Error: {httpEx.Message}");
                        if (httpEx.InnerException != null)
                        {
                            Console.WriteLine($"Inner exception: {httpEx.InnerException.Message}");
                        }
                        if (retryCount == 0)
                        {
                            Console.WriteLine($"Giving up on agent '{agent.Name}' after multiple retries.");
                        }
                        await Task.Delay(1000); // Wait 1 second before retrying
                    }
                    catch (Exception ex)
                    {
                        retryCount--;
                        Console.WriteLine($"Unexpected error for agent '{agent.Name}'. Retries left: {retryCount}. Error: {ex.Message}");
                        if (retryCount == 0)
                        {
                            Console.WriteLine($"Giving up on agent '{agent.Name}' after multiple retries.");
                        }
                        await Task.Delay(1000); // Wait 1 second before retrying
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize agent '{agent.Name}' at endpoint '{agent.Endpoint}': {ex.Message}");
            }
        }
    }

    public List<AgentInfo> GetAgents()
    {
        return _agents;
    }

    public async Task<Dictionary<string, string>> GetAgentHealthAsync()
    {
        var healthStatuses = new Dictionary<string, string>();

        foreach (var agent in _agents)
        {
            try
            {
                var healthUri = new Uri(new Uri(agent.Endpoint), "/healthz");
                var response = await _httpClient.GetAsync(healthUri);
                healthStatuses[agent.Name] = response.IsSuccessStatusCode ? "healthy" : "unhealthy";
            }
            catch
            {
                healthStatuses[agent.Name] = "unreachable";
            }
        }

        return healthStatuses;
    }

    public async Task<Dictionary<string, List<string>>> GetAvailableToolsAsync()
    {
        var toolsByAgent = new Dictionary<string, List<string>>();

        foreach (var agent in _agents)
        {
            try
            {
                Console.WriteLine($"Starting tool fetch for agent '{agent.Name}'...");
                Console.Out.Flush();

                var toolsUri = new Uri(new Uri(agent.Endpoint), "/mcp/tools");
                Console.WriteLine($"Constructed tools URI: {toolsUri}");
                Console.Out.Flush();

                var response = await _httpClient.GetAsync(toolsUri);
                Console.WriteLine($"Response status code for agent '{agent.Name}': {response.StatusCode}");
                Console.Out.Flush();

                response.EnsureSuccessStatusCode();

                var rawResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Raw response from agent '{agent.Name}': {rawResponse}");
                Console.Out.Flush();

                var toolNames = new List<string>();
                using var doc = JsonDocument.Parse(rawResponse);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tool in doc.RootElement.EnumerateArray())
                    {
                        if (tool.TryGetProperty("name", out var nameProp))
                            toolNames.Add(nameProp.GetString() ?? "");
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("tools", out var toolsProp) && toolsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tool in toolsProp.EnumerateArray())
                    {
                        if (tool.TryGetProperty("name", out var nameProp))
                            toolNames.Add(nameProp.GetString() ?? "");
                    }
                }
                toolsByAgent[agent.Name] = toolNames.Count > 0 ? toolNames : new List<string> { "No tools available" };
                Console.WriteLine($"Parsed tools for agent '{agent.Name}': {string.Join(", ", toolsByAgent[agent.Name])}");
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch tools for agent '{agent.Name}' at '{agent.Endpoint}': {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                toolsByAgent[agent.Name] = new List<string> { "Error fetching tools" };
            }
        }

        return toolsByAgent;
    }
}

public class AgentInfo
{
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
}

public class Tool
{
    public string Name { get; set; } = string.Empty;
}

// Define a class to represent the response structure
public class ToolResponse
{
    public List<Tool> Tools { get; set; } = new List<Tool>();
}

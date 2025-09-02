using System.Net.Http;
using System.Text.Json;

public class AgentRegistry
{
    private readonly HttpClient _httpClient;
    private readonly List<Agent> _agents;

    public AgentRegistry(HttpClient httpClient, List<Agent> agents)
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
                var response = await _httpClient.GetAsync(agent.Endpoint.Replace("/call", "/tools"));
                response.EnsureSuccessStatusCode();

                var tools = await JsonSerializer.DeserializeAsync<List<Tool>>(
                    await response.Content.ReadAsStreamAsync());

                agent.Capabilities = tools?.Select(t => t.Name).ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize agent {agent.Name}: {ex.Message}");
            }
        }
    }

    public List<Agent> GetAgents()
    {
        return _agents;
    }
}

public class Agent
{
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
}

public class Tool
{
    public string Name { get; set; } = string.Empty;
}

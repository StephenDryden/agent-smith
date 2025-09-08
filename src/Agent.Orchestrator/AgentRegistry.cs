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

				var toolsUri = new Uri(baseUri, "/mcp/tools");

				int retryCount = 3;
				while (retryCount > 0)
				{
					try
					{
						Console.WriteLine($"Attempting to fetch tools for agent '{agent.Name}' at {toolsUri}...");
						var response = await _httpClient.GetAsync(toolsUri);
						Console.WriteLine($"Response status code: {response.StatusCode}");

						response.EnsureSuccessStatusCode();

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
						await Task.Delay(1000);
					}
					catch (Exception ex)
					{
						retryCount--;
						Console.WriteLine($"Unexpected error for agent '{agent.Name}'. Retries left: {retryCount}. Error: {ex.Message}");
						if (retryCount == 0)
						{
							Console.WriteLine($"Giving up on agent '{agent.Name}' after multiple retries.");
						}
						await Task.Delay(1000);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to initialize agent '{agent.Name}' at endpoint '{agent.Endpoint}': {ex.Message}");
			}
		}
	}
	// ...existing code...
}

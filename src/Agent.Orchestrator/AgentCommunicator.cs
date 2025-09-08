using System.Net.Http;
using System.Text;
using System.Text.Json;

public class AgentCommunicator
{
	private readonly HttpClient _httpClient;

	public AgentCommunicator(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<AgentResponse> SendRequestAsync(AgentInfo agent, string userInput)
	{
		try
		{
			var content = new StringContent(userInput);
			var response = await _httpClient.PostAsync(agent.Endpoint, content);
			var responseBody = await response.Content.ReadAsStringAsync();
			return new AgentResponse(agent.Name, responseBody, null);
		}
		catch (Exception ex)
		{
			return new AgentResponse(agent.Name, null, ex.Message);
		}
	}

	public class ToolResponse
	{
		public List<Tool> Tools { get; set; } = new List<Tool>();
	}

	public async Task<List<string>> QueryToolsAsync(AgentInfo agent)
	{
		try
		{
			var toolsUri = new Uri(new Uri(agent.Endpoint), "/mcp/tools");
			var response = await _httpClient.GetAsync(toolsUri);
			response.EnsureSuccessStatusCode();

			var rawResponse = await response.Content.ReadAsStringAsync();
			Console.WriteLine($"Raw response from agent '{agent.Name}': {rawResponse}");

			using var doc = JsonDocument.Parse(rawResponse);

			if (doc.RootElement.ValueKind == JsonValueKind.Array)
			{
				var tools = new List<string>();
				foreach (var tool in doc.RootElement.EnumerateArray())
				{
					if (tool.TryGetProperty("name", out var nameProp))
						tools.Add(nameProp.GetString() ?? "");
				}
				return tools.Count > 0 ? tools : new List<string> { "No tools available" };
			}
			else if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("tools", out var toolsProp) && toolsProp.ValueKind == JsonValueKind.Array)
			{
				var tools = new List<string>();
				foreach (var tool in toolsProp.EnumerateArray())
				{
					if (tool.TryGetProperty("name", out var nameProp))
						tools.Add(nameProp.GetString() ?? "");
				}
				return tools.Count > 0 ? tools : new List<string> { "No tools available" };
			}
			else
			{
				return new List<string> { "No tools available" };
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error fetching tools for agent '{agent.Name}': {ex.Message}");
			return new List<string> { "Error fetching tools" };
		}
	}
}

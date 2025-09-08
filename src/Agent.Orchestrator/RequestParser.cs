using System.Linq;

public class RequestParser
{
	private readonly AgentRegistry _agentRegistry;

	public RequestParser(AgentRegistry agentRegistry)
	{
		_agentRegistry = agentRegistry;
	}

	public List<AgentInfo> ParseRequest(string userInput)
	{
		var keywords = userInput.ToLower().Split(' ');
		var matchingAgents = _agentRegistry.GetAgents()
			.Where(agent => agent.Capabilities.Any(capability =>
				keywords.Any(keyword => capability.Contains(keyword, StringComparison.OrdinalIgnoreCase))))
			.ToList();

		return matchingAgents;
	}
}

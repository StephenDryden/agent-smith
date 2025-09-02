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

    public async Task<AgentResponse> SendRequestAsync(Agent agent, string userInput)
    {
        try
        {
            var requestBody = JsonSerializer.Serialize(new { name = "ask", arguments = new { question = userInput } });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(agent.Endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return new AgentResponse(agent.Name, responseBody, null);
        }
        catch (Exception ex)
        {
            return new AgentResponse(agent.Name, null, ex.Message);
        }
    }
}

public record AgentResponse(string AgentName, string? Response, string? Error);

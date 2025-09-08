using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Agent.Orchestrator;

namespace Agent.Orchestrator.Tests
{
	public class AgentCommunicatorTests
	{
		[Fact]
		public async Task SendRequestAsync_ShouldReturnResponse_WhenAgentResponds()
		{
			// Arrange
			var mockHttpClient = new Mock<HttpClient>();
			var communicator = new AgentCommunicator(mockHttpClient.Object);
			var agent = new AgentInfo
			{
				Name = "TestAgent",
				Endpoint = "http://example.com/mcp/call"
			};

			var expectedResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"success\"}")
			};

			mockHttpClient
				.Setup(client => client.SendAsync(It.IsAny<HttpRequestMessage>()))
				.ReturnsAsync(expectedResponse);

			// Act
			var response = await communicator.SendRequestAsync(agent, "test input");

			// Assert
			Assert.NotNull(response);
			Assert.Equal("{\"status\":\"success\"}", response.Response);
		}

		[Fact]
		public async Task SendRequestAsync_ShouldThrowException_WhenAgentFails()
		{
			// Arrange
			var mockHttpClient = new Mock<HttpClient>();
			var communicator = new AgentCommunicator(mockHttpClient.Object);
			var agent = new AgentInfo
			{
				Name = "TestAgent",
				Endpoint = "http://example.com/mcp/call"
			};

			mockHttpClient
				.Setup(client => client.SendAsync(It.IsAny<HttpRequestMessage>()))
				.ThrowsAsync(new HttpRequestException("Agent not reachable"));

			// Act & Assert
			await Assert.ThrowsAsync<HttpRequestException>(async () => await communicator.SendRequestAsync(agent, "test input"));
		}

		[Fact]
		public async Task QueryToolsAsync_ShouldReturnTools_WhenAgentResponds()
		{
			// Arrange
			var mockHttpClient = new Mock<HttpClient>();
			var communicator = new AgentCommunicator(mockHttpClient.Object);
			var agent = new AgentInfo
			{
				Name = "TestAgent",
				Endpoint = "http://example.com"
			};

			var expectedTools = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent("[{\"Name\":\"Tool1\"},{\"Name\":\"Tool2\"}]")
			};

			mockHttpClient
				.Setup(client => client.GetAsync(It.IsAny<string>()))
				.ReturnsAsync(expectedTools);

			// ...existing code...
		}
	}
}

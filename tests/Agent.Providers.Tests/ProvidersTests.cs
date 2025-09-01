using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Agent.Core;
using Agent.Providers;
using FluentAssertions;
using Xunit;

namespace Agent.Providers.Tests;

public class ProvidersTests
{
    [Fact]
    public async Task MockProvider_Echoes_Last_User()
    {
        var p = new MockModelsProvider();
        var msg = await p.GetChatCompletionAsync("model", new()
        {
            new ChatMessage("system", "persona"),
            new ChatMessage("user", "Hello")
        }, 0.1, 1.0);
        msg.Should().Contain("Hello");
    }

    private sealed class FakeHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    [Fact]
    public async Task GitHubProvider_Parses_Choices_Message_Content()
    {
        var body = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = "hi!" } } }
        });
        var resp = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        using var provider = new GitHubModelsProvider("token", "https://example", new FakeHandler(resp));
        var content = await provider.GetChatCompletionAsync("model", new() { new ChatMessage("user", "q") }, 0.2, 1.0);
        content.Should().Be("hi!");
    }
}

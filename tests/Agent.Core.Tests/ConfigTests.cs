using System.Text.Json;
using Agent.Core;
using FluentAssertions;
using Xunit;

namespace Agent.Core.Tests;

public class ConfigTests
{
    [Fact]
    public void Defaults_Are_Set_For_Config_Sections()
    {
        var cfg = new AgentConfig
        {
            Agent = new AgentSection(),
            Model = new ModelSection(),
            Mcp = new McpSection()
        };

        cfg.Agent.Name.Should().Be("agent");
        cfg.Agent.SystemPrompt.Should().NotBeNullOrWhiteSpace();
        cfg.Model.Provider.Should().Be("github-models");
        cfg.Model.ModelId.Should().NotBeNullOrWhiteSpace();
        cfg.Model.Parameters.Temperature.Should().BeGreaterThanOrEqualTo(0);
        cfg.Mcp.Transport.Should().NotBeNullOrWhiteSpace();
        cfg.Runtime.Port.Should().BeGreaterThan(0);
        cfg.Security.ApiAuthEnabled.Should().BeFalse();
    }

    [Fact]
    public void ChatRequest_Serializes_And_Deserializes()
    {
        var req = new ChatRequest
        {
            Messages = new() { new ChatMessage("user", "hi") }
        };
        var json = JsonSerializer.Serialize(req);
        var round = JsonSerializer.Deserialize<ChatRequest>(json);
        round.Should().NotBeNull();
        round!.Messages.Should().ContainSingle(m => m.Role == "user" && m.Content == "hi");
    }
}

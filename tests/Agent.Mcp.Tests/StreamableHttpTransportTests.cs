using System.Reflection;
using System.Text.Json;
using Agent.Mcp.Protocol;
using FluentAssertions;
using Xunit;

namespace Agent.Mcp.Tests;

public class StreamableHttpTransportTests
{
    private static IEnumerable<object> InvokeParse(string json)
    {
        var t = typeof(Agent.Mcp.Transport.StreamableHttpTransport);
        var m = t.GetMethod("ParseJsonRpc", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (IEnumerable<object>)m.Invoke(null, new object[] { json })!;
    }

    [Fact]
    public void Parse_Single_Response()
    {
        var json = "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"result\":{\"ok\":true}}";
        var items = InvokeParse(json).ToList();
        items.Should().ContainSingle();
        items.Single().Should().BeOfType<JsonRpcResponse>()
            .Which.Result.Should().NotBeNull();
    }

    [Fact]
    public void Parse_Array_Mixed_Objects()
    {
        var json = "[{\"jsonrpc\":\"2.0\",\"id\":\"a\",\"result\":{}},{\"jsonrpc\":\"2.0\",\"method\":\"notify\",\"params\":{}}]";
        var items = InvokeParse(json).ToList();
        items.Should().HaveCount(2);
        items[0].Should().BeOfType<JsonRpcResponse>();
        items[1].Should().BeOfType<JsonRpcNotification>();
    }

    [Fact]
    public void Parse_Fallback_To_JsonElement_For_Unknown()
    {
        var json = "{\"foo\":123}";
        var items = InvokeParse(json).ToList();
        items.Should().ContainSingle();
        items[0].Should().BeOfType<JsonElement>();
    }
}

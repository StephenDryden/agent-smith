using Agent.Mcp.Protocol;

namespace Agent.Mcp.Transport;

public interface IMcpTransport : IAsyncDisposable
{
    Task InitializeAsync(CancellationToken ct = default);
    Task SendAsync(IEnumerable<JsonRpcRequest> requests, CancellationToken ct = default);
    IAsyncEnumerable<object> ReceiveAsync(CancellationToken ct = default);
}

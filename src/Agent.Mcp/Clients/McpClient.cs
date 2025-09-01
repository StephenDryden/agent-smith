using Agent.Core;
using Agent.Mcp.Protocol;
using Agent.Mcp.Transport;

namespace Agent.Mcp.Clients;

public sealed class McpClient : IAsyncDisposable
{
    private readonly IMcpTransport _transport;

    public McpClient(AgentConfig cfg)
    {
        _transport = cfg.Mcp.Transport.Equals("streamable-http", StringComparison.OrdinalIgnoreCase)
            ? new StreamableHttpTransport(cfg.Mcp.Http?.Url ?? throw new ArgumentException("mcp.http.url missing"), cfg.Mcp.Http?.AllowSse ?? true, cfg.Mcp.Http?.TimeoutMs ?? 60000)
            : new StdioTransport(cfg.Mcp.Stdio?.Command ?? "npx", cfg.Mcp.Stdio?.Args ?? Array.Empty<string>(), cfg.Mcp.Stdio?.Env, cfg.Mcp.Stdio?.WorkingDir);
    }

    public Task InitializeAsync(CancellationToken ct = default) => _transport.InitializeAsync(ct);

    public Task SendAsync(IEnumerable<JsonRpcRequest> requests, CancellationToken ct = default) => _transport.SendAsync(requests, ct);

    public IAsyncEnumerable<object> ReceiveAsync(CancellationToken ct = default) => _transport.ReceiveAsync(ct);

    public ValueTask DisposeAsync() => _transport.DisposeAsync();
}

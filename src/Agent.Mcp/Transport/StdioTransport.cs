using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Agent.Mcp.Protocol;

namespace Agent.Mcp.Transport;

public sealed class StdioTransport(string command, string[] args, IReadOnlyDictionary<string,string>? env = null, string? workingDir = null) : IMcpTransport
{
    private Process? _proc;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;

    public Task InitializeAsync(CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(command)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDir ?? Environment.CurrentDirectory
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        if (env is not null)
        {
            foreach (var kv in env)
                psi.Environment[kv.Key] = kv.Value;
        }

        _proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _proc.Start();
        _stdin = _proc.StandardInput;
        _stdout = _proc.StandardOutput;
        return Task.CompletedTask;
    }

    public async Task SendAsync(IEnumerable<JsonRpcRequest> requests, CancellationToken ct = default)
    {
        if (_stdin is null) throw new InvalidOperationException("Transport not initialized");
        foreach (var req in requests)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(req);
            await _stdin.WriteLineAsync(json.AsMemory(), ct).ConfigureAwait(false);
            await _stdin.FlushAsync().ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<object> ReceiveAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_stdout is null) yield break;
        string? line;
        while (!ct.IsCancellationRequested && (line = await _stdout.ReadLineAsync()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            foreach (var parsed in ParseLine(line))
                yield return parsed;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_stdin is not null) await _stdin.DisposeAsync();
            _stdout?.Dispose();
            if (_proc is { HasExited: false }) _proc.Kill(true);
            _proc?.Dispose();
        }
        catch { }
    }

    private static IEnumerable<object> ParseLine(string line)
    {
        List<object> results = new();
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in root.EnumerateArray())
                    results.AddRange(ParseElement(el));
            }
            else
            {
                results.AddRange(ParseElement(root));
            }
        }
        catch
        {
            results.Add(line);
        }
        return results;
    }

    private static IEnumerable<object> ParseElement(JsonElement el)
    {
        List<object> results = new();
        try
        {
            var hasId = el.TryGetProperty("id", out _);
            var hasMethod = el.TryGetProperty("method", out _);
            if (hasId)
            {
                var resp = el.Deserialize<JsonRpcResponse>();
                if (resp is not null) { results.Add(resp); return results; }
            }
            if (!hasId && hasMethod)
            {
                var notif = el.Deserialize<JsonRpcNotification>();
                if (notif is not null) { results.Add(notif); return results; }
            }
        }
        catch { }
        results.Add(el.Clone());
        return results;
    }
}

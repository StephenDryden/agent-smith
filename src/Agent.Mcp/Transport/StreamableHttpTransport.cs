using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Agent.Mcp.Protocol;

namespace Agent.Mcp.Transport;

public sealed class StreamableHttpTransport(string url, bool allowSse = true, int timeoutMs = 60000) : IMcpTransport
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };
    private readonly string _url = url;
    private readonly bool _allowSse = allowSse;

    public Task InitializeAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask; // Initialization will occur via an explicit initialize request
    }

    public async Task SendAsync(IEnumerable<JsonRpcRequest> requests, CancellationToken ct = default)
    {
        var payload = requests.Count() == 1 ? JsonSerializer.Serialize(requests.First()) : JsonSerializer.Serialize(requests);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = content
        };
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (_allowSse)
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        // Response will be consumed by ReceiveAsync stream reader.
        var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        _pendingResponse = resp;
    }

    private HttpResponseMessage? _pendingResponse;

    public async IAsyncEnumerable<object> ReceiveAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_pendingResponse is null)
            yield break;

        var resp = _pendingResponse;
        _pendingResponse = null;

        if (resp.Content.Headers.ContentType?.MediaType == "text/event-stream")
        {
            // TODO: parse SSE events and yield JSON-RPC messages
            await foreach (var item in ReadSseAsync(resp, ct))
                yield return item;
        }
        else
        {
            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var doc = JsonDocument.Parse(json);
            yield return doc; // placeholder
        }
    }

    private static async IAsyncEnumerable<object> ReadSseAsync(HttpResponseMessage resp, [EnumeratorCancellation] CancellationToken ct)
    {
        using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        var sb = new StringBuilder();
        while (!reader.EndOfStream && (line = await reader.ReadLineAsync()) is not null)
        {
            if (ct.IsCancellationRequested) yield break;
            if (line.StartsWith("data:"))
            {
                sb.AppendLine(line.AsSpan(5).Trim().ToString());
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                var data = sb.ToString().Trim();
                sb.Clear();
                if (!string.IsNullOrEmpty(data))
                    yield return data; // placeholder yields raw data lines
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        _pendingResponse?.Dispose();
        return ValueTask.CompletedTask;
    }
}

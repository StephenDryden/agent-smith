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
            await foreach (var item in ReadSseAsync(resp, ct))
                yield return item;
        }
        else
        {
            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            foreach (var parsed in ParseJsonRpc(json))
                yield return parsed;
        }
    }

    private static async IAsyncEnumerable<object> ReadSseAsync(HttpResponseMessage resp, [EnumeratorCancellation] CancellationToken ct)
    {
        using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        var sb = new StringBuilder();
        string? eventName = null;
        while (!reader.EndOfStream && (line = await reader.ReadLineAsync()) is not null)
        {
            if (ct.IsCancellationRequested) yield break;
            if (line.StartsWith("event:"))
            {
                // event: <name>
                eventName = line.AsSpan(6).Trim().ToString();
            }
            else if (line.StartsWith("data:"))
            {
                sb.AppendLine(line.AsSpan(5).Trim().ToString());
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                var data = sb.ToString().Trim();
                sb.Clear();
                if (string.Equals(data, "[DONE]", StringComparison.OrdinalIgnoreCase))
                {
                    // graceful end of stream
                    yield break;
                }
                if (!string.IsNullOrEmpty(data))
                {
                    foreach (var parsed in ParseJsonRpc(data))
                        yield return parsed;
                }
                eventName = null;
            }
        }
    }

    private static IEnumerable<object> ParseJsonRpc(string jsonOrLine)
    {
        // The response can be a single object or an array of objects. Objects can be responses or notifications.
        List<object> results = new();
        if (string.IsNullOrWhiteSpace(jsonOrLine)) return results;

        using JsonDocument? doc = SafeParse(jsonOrLine);
        if (doc is null) { results.Add(jsonOrLine); return results; }

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in doc.RootElement.EnumerateArray())
                results.AddRange(ParseJsonElement(el));
        }
        else
        {
            results.AddRange(ParseJsonElement(doc.RootElement));
        }
        return results;
    }

    private static IEnumerable<object> ParseJsonElement(JsonElement el)
    {
        // Heuristics: if property "id" exists (null or string) and one of result/error present => response
        // if no id but method present => notification
        List<object> results = new();
        try
        {
            if (el.ValueKind == JsonValueKind.Object)
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
        }
        catch { }
        // Fallback to raw JsonDocument for unknown shapes
        results.Add(el.Clone());
        return results;
    }

    private static JsonDocument? SafeParse(string json)
    {
        try { return JsonDocument.Parse(json); } catch { return null; }
    }

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        _pendingResponse?.Dispose();
        return ValueTask.CompletedTask;
    }
}

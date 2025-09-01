using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Agent.Core;

namespace Agent.Providers;

public sealed class GitHubModelsProvider(string token, string? baseUrl = null, HttpMessageHandler? handler = null) : IModelProvider, IDisposable
{
    private readonly HttpClient _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
    private readonly string _base = string.IsNullOrWhiteSpace(baseUrl) ? "https://models.github.ai" : baseUrl.TrimEnd('/');
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async Task<string> GetChatCompletionAsync(string modelId, List<ChatMessage> messages, double temperature, double topP, CancellationToken ct = default)
    {
        // Build OpenAI-style payload
        var payload = new
        {
            model = modelId,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            temperature,
            top_p = topP,
            stream = false
        };
        var json = JsonSerializer.Serialize(payload, _json);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, _base + "/inference/chat/completions")
        {
            Content = content
        };
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        // Expect choices[0].message.content
        var choices = root.GetProperty("choices");
        if (choices.GetArrayLength() == 0) return string.Empty;
        var msg = choices[0].GetProperty("message");
        var contentStr = msg.GetProperty("content").GetString() ?? string.Empty;
        return contentStr;
    }

    public void Dispose() => _http.Dispose();
}

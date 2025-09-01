using Agent.Core;

namespace Agent.Providers;

public sealed class MockModelsProvider : IModelProvider
{
    public Task<string> GetChatCompletionAsync(string modelId, List<ChatMessage> messages, double temperature, double topP, CancellationToken ct = default)
    {
        var lastUser = messages.LastOrDefault(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content ?? string.Empty;
        var sys = messages.FirstOrDefault(m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))?.Content ?? string.Empty;
        var reply = $"mock: {lastUser}".Trim();
        if (!string.IsNullOrWhiteSpace(sys)) reply += " | sys:" + (sys.Length > 60 ? sys[..60] + "…" : sys);
        return Task.FromResult(reply);
    }
}

using Agent.Core;

namespace Agent.Providers;

public interface IModelProvider
{
    Task<string> GetChatCompletionAsync(string modelId, List<ChatMessage> messages, double temperature, double topP, CancellationToken ct = default);
}

namespace Agent.Core;

public class AgentConfig
{
    public required AgentSection Agent { get; set; }
    public required ModelSection Model { get; set; }
    public required McpSection Mcp { get; set; }
    public RuntimeSection Runtime { get; set; } = new();
    public SecuritySection Security { get; set; } = new();
}

public class AgentSection
{
    public string Name { get; set; } = "agent";
    public string SystemPrompt { get; set; } = "You are helpful.";
}

public class ModelSection
{
    public string Provider { get; set; } = "github-models";
    public string ModelId { get; set; } = "openai/gpt-4o-mini";
    public ModelParameters Parameters { get; set; } = new();
}

public class ModelParameters
{
    public double Temperature { get; set; } = 0.2;
    public double TopP { get; set; } = 1.0;
}

public class McpSection
{
    public string Transport { get; set; } = "stdio"; // or streamable-http
    public McpHttp? Http { get; set; }
    public McpStdio? Stdio { get; set; }
    public McpSession Session { get; set; } = new();
}

public class McpHttp
{
    public string Url { get; set; } = string.Empty;
    public bool AllowSse { get; set; } = true;
    public int TimeoutMs { get; set; } = 60000;
}

public class McpStdio
{
    public string Command { get; set; } = "npx";
    public string[] Args { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> Env { get; set; } = new();
    public string? WorkingDir { get; set; }
}

public class McpSession
{
    public bool UseSessionHeader { get; set; } = true;
    public bool Resume { get; set; } = false;
}

public class RuntimeSection
{
    public int Port { get; set; } = 8080;
}

public class SecuritySection
{
    public bool ValidateOrigin { get; set; } = false;
    public bool ApiAuthEnabled { get; set; } = false;
}

public record ChatMessage(string Role, string Content);

public class ChatRequest
{
    public List<ChatMessage> Messages { get; set; } = new();
}

public class ChatResponse
{
    public ChatMessage Message { get; set; } = new("assistant", string.Empty);
}

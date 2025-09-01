namespace Agent.Template;

public sealed class McpCallRequest
{
    public string Name { get; set; } = string.Empty;
    public object? Arguments { get; set; }
}

using System.Text.Json.Serialization;

namespace Agent.Mcp.Protocol;

public class JsonRpcError
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("data")] public object? Data { get; set; }
}

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")] public string Version { get; set; } = "2.0";
    [JsonPropertyName("result")] public object? Result { get; set; }
    [JsonPropertyName("error")] public JsonRpcError? Error { get; set; }
    [JsonPropertyName("id")] public string? Id { get; set; }
}

public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")] public string Version { get; set; } = "2.0";
    [JsonPropertyName("method")] public string Method { get; set; } = string.Empty;
    [JsonPropertyName("params")] public object? Params { get; set; }
    [JsonPropertyName("id")] public string? Id { get; set; }
}

public class JsonRpcNotification
{
    [JsonPropertyName("jsonrpc")] public string Version { get; set; } = "2.0";
    [JsonPropertyName("method")] public string Method { get; set; } = string.Empty;
    [JsonPropertyName("params")] public object? Params { get; set; }
}

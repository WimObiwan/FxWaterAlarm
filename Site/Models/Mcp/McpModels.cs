using System.Text.Json.Serialization;

namespace Site.Models.Mcp;

// JSON-RPC 2.0 Request/Response
public record McpRequest
{
    public required string Jsonrpc { get; init; } = "2.0";
    public required string Method { get; init; }
    public object? Params { get; init; }
    public object? Id { get; init; }
}

public record McpResponse
{
    public string Jsonrpc { get; init; } = "2.0";
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpError? Error { get; init; }
    
    public object? Id { get; init; }
}

public record McpError
{
    public int Code { get; init; }
    public required string Message { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; init; }
}

// Initialize Response
public record InitializeResult
{
    public required string ProtocolVersion { get; init; }
    public required Capabilities Capabilities { get; init; }
    public required ServerInfo ServerInfo { get; init; }
}

public record ServerInfo
{
    public required string Name { get; init; }
    public required string Version { get; init; }
}

public record Capabilities
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResourcesCapability? Resources { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolsCapability? Tools { get; init; }
}

public record ResourcesCapability
{
    public bool Subscribe { get; init; }
    public bool ListChanged { get; init; }
}

public record ToolsCapability
{
    public bool ListChanged { get; init; }
}

// Resources
public record ResourceListResult
{
    public required List<Resource> Resources { get; init; }
}

public record Resource
{
    public required string Uri { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string MimeType { get; init; }
}

public record ResourceReadRequest
{
    public required string Uri { get; init; }
}

public record ResourceReadResult
{
    public required List<ResourceContent> Contents { get; init; }
}

public record ResourceContent
{
    public required string Uri { get; init; }
    public required string MimeType { get; init; }
    public string? Text { get; init; }
}

// Tools
public record ToolListResult
{
    public required List<Tool> Tools { get; init; }
}

public record Tool
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required object InputSchema { get; init; }
}

public record ToolCallRequest
{
    public required string Name { get; init; }
    public object? Arguments { get; init; }
}

public record ToolCallResult
{
    public required List<ToolContent> Content { get; init; }
    public bool? IsError { get; init; }
}

public record ToolContent
{
    public required string Type { get; init; }
    public required string Text { get; init; }
}

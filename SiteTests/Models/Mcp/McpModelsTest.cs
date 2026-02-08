using System.Text.Json;
using System.Text.Json.Serialization;
using Site.Models.Mcp;
using Xunit;

namespace SiteTests.Models.Mcp;

public class McpModelsTest
{
    // --- McpRequest ---

    [Fact]
    public void McpRequest_CanBeCreated()
    {
        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Method = "initialize",
            Params = new { clientInfo = "test" },
            Id = 1
        };

        Assert.Equal("2.0", request.Jsonrpc);
        Assert.Equal("initialize", request.Method);
        Assert.NotNull(request.Params);
        Assert.Equal(1, request.Id);
    }

    [Fact]
    public void McpRequest_SerializesAndDeserializes()
    {
        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Method = "tools/list",
            Id = 42
        };

        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<McpRequest>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("2.0", deserialized!.Jsonrpc);
        Assert.Equal("tools/list", deserialized.Method);
        Assert.Null(deserialized.Params);
    }

    // --- McpResponse ---

    [Fact]
    public void McpResponse_OmitsNullResultAndError()
    {
        var response = new McpResponse
        {
            Id = 1
        };

        var json = JsonSerializer.Serialize(response);
        Assert.DoesNotContain("\"Result\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Error\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void McpResponse_IncludesResultWhenSet()
    {
        var response = new McpResponse
        {
            Result = new { data = "test" },
            Id = 1
        };

        var json = JsonSerializer.Serialize(response);
        Assert.Contains("\"data\"", json);
    }

    [Fact]
    public void McpResponse_IncludesErrorWhenSet()
    {
        var response = new McpResponse
        {
            Error = new McpError { Code = -32600, Message = "Invalid Request" },
            Id = 1
        };

        var json = JsonSerializer.Serialize(response);
        Assert.Contains("Invalid Request", json);
    }

    // --- McpError ---

    [Fact]
    public void McpError_OmitsNullData()
    {
        var error = new McpError { Code = -32601, Message = "Method not found" };

        var json = JsonSerializer.Serialize(error);
        Assert.DoesNotContain("\"Data\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void McpError_IncludesDataWhenSet()
    {
        var error = new McpError
        {
            Code = -32602,
            Message = "Invalid params",
            Data = "extra info"
        };

        var json = JsonSerializer.Serialize(error);
        Assert.Contains("extra info", json);
    }

    // --- InitializeResult ---

    [Fact]
    public void InitializeResult_CanBeCreated()
    {
        var result = new InitializeResult
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new Capabilities
            {
                Resources = new ResourcesCapability { Subscribe = false, ListChanged = true },
                Tools = new ToolsCapability { ListChanged = false }
            },
            ServerInfo = new ServerInfo { Name = "Test", Version = "1.0.0" }
        };

        Assert.Equal("2024-11-05", result.ProtocolVersion);
        Assert.False(result.Capabilities.Resources!.Subscribe);
        Assert.True(result.Capabilities.Resources.ListChanged);
        Assert.False(result.Capabilities.Tools!.ListChanged);
        Assert.Equal("Test", result.ServerInfo.Name);
    }

    // --- Capabilities with null sub-capabilities ---

    [Fact]
    public void Capabilities_OmitsNullResourcesAndTools()
    {
        var capabilities = new Capabilities();

        var json = JsonSerializer.Serialize(capabilities);
        Assert.DoesNotContain("\"Resources\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Tools\"", json, StringComparison.OrdinalIgnoreCase);
    }

    // --- Resource ---

    [Fact]
    public void Resource_CanBeCreated()
    {
        var resource = new Resource
        {
            Uri = "docs://readme.md",
            Name = "README",
            Description = "Main readme",
            MimeType = "text/markdown"
        };

        Assert.Equal("docs://readme.md", resource.Uri);
        Assert.Equal("README", resource.Name);
        Assert.Equal("Main readme", resource.Description);
    }

    // --- ResourceReadRequest ---

    [Fact]
    public void ResourceReadRequest_CanBeCreated()
    {
        var request = new ResourceReadRequest { Uri = "docs://test.md" };
        Assert.Equal("docs://test.md", request.Uri);
    }

    // --- ResourceReadResult ---

    [Fact]
    public void ResourceReadResult_ContainsContents()
    {
        var result = new ResourceReadResult
        {
            Contents = new List<ResourceContent>
            {
                new ResourceContent { Uri = "docs://test.md", MimeType = "text/markdown", Text = "# Hello" }
            }
        };

        Assert.Single(result.Contents);
        Assert.Equal("# Hello", result.Contents[0].Text);
    }

    // --- Tool ---

    [Fact]
    public void Tool_CanBeCreated()
    {
        var tool = new Tool
        {
            Name = "search",
            Description = "Search docs",
            InputSchema = new { type = "object" }
        };

        Assert.Equal("search", tool.Name);
        Assert.Equal("Search docs", tool.Description);
    }

    // --- ToolCallRequest ---

    [Fact]
    public void ToolCallRequest_CanBeCreated()
    {
        var request = new ToolCallRequest
        {
            Name = "search_documentation",
            Arguments = new { query = "setup" }
        };

        Assert.Equal("search_documentation", request.Name);
        Assert.NotNull(request.Arguments);
    }

    // --- ToolCallResult ---

    [Fact]
    public void ToolCallResult_CanBeCreated()
    {
        var result = new ToolCallResult
        {
            Content = new List<ToolContent>
            {
                new ToolContent { Type = "text", Text = "Found results" }
            },
            IsError = false
        };

        Assert.Single(result.Content);
        Assert.False(result.IsError);
    }

    [Fact]
    public void ToolCallResult_IsErrorCanBeNull()
    {
        var result = new ToolCallResult
        {
            Content = new List<ToolContent>()
        };

        Assert.Null(result.IsError);
    }

    // --- ToolContent ---

    [Fact]
    public void ToolContent_CanBeCreated()
    {
        var content = new ToolContent { Type = "text", Text = "hello" };
        Assert.Equal("text", content.Type);
        Assert.Equal("hello", content.Text);
    }

    // --- ToolListResult ---

    [Fact]
    public void ToolListResult_CanBeCreated()
    {
        var result = new ToolListResult
        {
            Tools = new List<Tool>
            {
                new Tool { Name = "t1", Description = "d1", InputSchema = new { } }
            }
        };

        Assert.Single(result.Tools);
    }

    // --- ResourceListResult ---

    [Fact]
    public void ResourceListResult_CanBeCreated()
    {
        var result = new ResourceListResult
        {
            Resources = new List<Resource>
            {
                new Resource { Uri = "u", Name = "n", MimeType = "text/plain" }
            }
        };

        Assert.Single(result.Resources);
    }

    // --- Full roundtrip ---

    [Fact]
    public void McpResponse_FullRoundtrip()
    {
        var response = new McpResponse
        {
            Jsonrpc = "2.0",
            Result = new InitializeResult
            {
                ProtocolVersion = "2024-11-05",
                Capabilities = new Capabilities(),
                ServerInfo = new ServerInfo { Name = "WA", Version = "1.0" }
            },
            Id = 1
        };

        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<McpResponse>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("2.0", deserialized!.Jsonrpc);
        Assert.NotNull(deserialized.Result);
        Assert.Null(deserialized.Error);
    }
}

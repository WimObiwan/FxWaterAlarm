using System.Text.Json;
using Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Site.Controllers;
using Site.Models.Mcp;
using Site.Services;
using Xunit;

namespace SiteTests.Controllers;

public class McpControllerTest
{
    private class FakeVersionInfo : IVersionInfo
    {
        public string? Version { get; set; } = "1.2.3";
        public string DotNetCoreVersion => "10.0.0";
    }

    private class FakeMcpDocumentationService : IMcpDocumentationService
    {
        public List<Resource> Resources { get; set; } = new();
        public ResourceContent? ReadResult { get; set; }
        public List<ToolContent> SearchResult { get; set; } = new();
        public bool ThrowFileNotFound { get; set; }
        public bool ThrowUnauthorized { get; set; }

        public Task<List<Resource>> ListResourcesAsync() => Task.FromResult(Resources);

        public Task<ResourceContent> ReadResourceAsync(string uri)
        {
            if (ThrowFileNotFound) throw new FileNotFoundException("not found");
            if (ThrowUnauthorized) throw new UnauthorizedAccessException("denied");
            return Task.FromResult(ReadResult!);
        }

        public Task<List<ToolContent>> SearchDocumentationAsync(string query)
            => Task.FromResult(SearchResult);
    }

    private static McpController CreateController(
        IMcpDocumentationService? docService = null,
        IVersionInfo? versionInfo = null)
    {
        return new McpController(
            docService ?? new FakeMcpDocumentationService(),
            versionInfo ?? new FakeVersionInfo(),
            NullLogger<McpController>.Instance);
    }

    private static McpRequest CreateRequest(string method, object? paramsObj = null, object? id = null)
    {
        object? parsedParams = null;
        if (paramsObj != null)
        {
            // Serialize and deserialize to get JsonElement
            var json = JsonSerializer.Serialize(paramsObj);
            parsedParams = JsonSerializer.Deserialize<JsonElement>(json);
        }

        return new McpRequest
        {
            Jsonrpc = "2.0",
            Method = method,
            Params = parsedParams,
            Id = id ?? 1
        };
    }

    private static McpResponse GetResponse(IActionResult result)
    {
        var okResult = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<McpResponse>(okResult.Value);
    }

    // --- Initialize ---

    [Fact]
    public async Task Initialize_ReturnsServerInfo()
    {
        var controller = CreateController(versionInfo: new FakeVersionInfo { Version = "2.0.0" });
        var request = CreateRequest("initialize");

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.Null(response.Error);
        Assert.NotNull(response.Result);

        var json = JsonSerializer.Serialize(response.Result);
        var initResult = JsonSerializer.Deserialize<InitializeResult>(json);
        Assert.Equal("2024-11-05", initResult!.ProtocolVersion);
        Assert.Equal("WaterAlarm Documentation Server", initResult.ServerInfo.Name);
        Assert.Equal("2.0.0", initResult.ServerInfo.Version);
    }

    [Fact]
    public async Task Initialize_UsesDefaultVersion_WhenNull()
    {
        var controller = CreateController(versionInfo: new FakeVersionInfo { Version = null });
        var request = CreateRequest("initialize");

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        var json = JsonSerializer.Serialize(response.Result);
        var initResult = JsonSerializer.Deserialize<InitializeResult>(json);
        Assert.Equal("1.0.0", initResult!.ServerInfo.Version);
    }

    [Fact]
    public async Task Initialize_ReturnsCapabilities()
    {
        var controller = CreateController();
        var request = CreateRequest("initialize");

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        var json = JsonSerializer.Serialize(response.Result);
        var initResult = JsonSerializer.Deserialize<InitializeResult>(json);
        Assert.NotNull(initResult!.Capabilities.Resources);
        Assert.NotNull(initResult.Capabilities.Tools);
    }

    // --- resources/list ---

    [Fact]
    public async Task ResourcesList_ReturnsResources()
    {
        var docService = new FakeMcpDocumentationService
        {
            Resources = new List<Resource>
            {
                new Resource { Uri = "docs://test.md", Name = "test", MimeType = "text/markdown" }
            }
        };
        var controller = CreateController(docService);
        var request = CreateRequest("resources/list");

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
    }

    // --- resources/read ---

    [Fact]
    public async Task ResourcesRead_ReturnsContent()
    {
        var docService = new FakeMcpDocumentationService
        {
            ReadResult = new ResourceContent { Uri = "docs://test.md", MimeType = "text/markdown", Text = "# Hello" }
        };
        var controller = CreateController(docService);
        var request = CreateRequest("resources/read", new { uri = "docs://test.md" });

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.Null(response.Error);
    }

    [Fact]
    public async Task ResourcesRead_ReturnsError_WhenNoParams()
    {
        var controller = CreateController();
        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Method = "resources/read",
            Params = null,
            Id = 1
        };

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.NotNull(response.Error);
        Assert.Equal(-32602, response.Error!.Code);
    }

    [Fact]
    public async Task ResourcesRead_ReturnsError_WhenFileNotFound()
    {
        var docService = new FakeMcpDocumentationService { ThrowFileNotFound = true };
        var controller = CreateController(docService);
        var request = CreateRequest("resources/read", new { uri = "docs://missing.md" });

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.NotNull(response.Error);
        Assert.Equal(-32001, response.Error!.Code);
    }

    [Fact]
    public async Task ResourcesRead_ReturnsError_WhenUnauthorized()
    {
        var docService = new FakeMcpDocumentationService { ThrowUnauthorized = true };
        var controller = CreateController(docService);
        var request = CreateRequest("resources/read", new { uri = "docs://../../secret" });

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.NotNull(response.Error);
        Assert.Equal(-32002, response.Error!.Code);
    }

    // --- tools/list ---

    [Fact]
    public async Task ToolsList_ReturnsSearchTool()
    {
        var controller = CreateController();
        var request = CreateRequest("tools/list");

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.Null(response.Error);
        Assert.NotNull(response.Result);

        var json = JsonSerializer.Serialize(response.Result);
        var toolList = JsonSerializer.Deserialize<ToolListResult>(json);
        Assert.Single(toolList!.Tools);
        Assert.Equal("search_documentation", toolList.Tools[0].Name);
    }

    // --- tools/call ---

    [Fact]
    public async Task ToolsCall_SearchReturnsResults()
    {
        var docService = new FakeMcpDocumentationService
        {
            SearchResult = new List<ToolContent>
            {
                new ToolContent { Type = "text", Text = "Found results" }
            }
        };
        var controller = CreateController(docService);
        var request = CreateRequest("tools/call",
            new { name = "search_documentation", arguments = new { query = "test" } });

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.Null(response.Error);
    }

    [Fact]
    public async Task ToolsCall_ReturnsError_WhenNoParams()
    {
        var controller = CreateController();
        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Method = "tools/call",
            Params = null,
            Id = 1
        };

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.NotNull(response.Error);
        Assert.Equal(-32602, response.Error!.Code);
    }

    [Fact]
    public async Task ToolsCall_ReturnsError_WhenUnknownTool()
    {
        var controller = CreateController();
        var request = CreateRequest("tools/call",
            new { name = "unknown_tool", arguments = new { query = "test" } });

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.NotNull(response.Error);
        Assert.Equal(-32602, response.Error!.Code);
    }

    [Fact]
    public async Task ToolsCall_ReturnsError_WhenMissingQuery()
    {
        var controller = CreateController();
        var request = CreateRequest("tools/call",
            new { name = "search_documentation" });

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.NotNull(response.Error);
    }

    [Fact]
    public async Task ToolsCall_ReturnsError_WhenEmptyQuery()
    {
        var controller = CreateController();
        var request = CreateRequest("tools/call",
            new { name = "search_documentation", arguments = new { query = "  " } });

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.NotNull(response.Error);
    }

    // --- Unknown method ---

    [Fact]
    public async Task HandleRequest_ReturnsMethodNotFound_ForUnknownMethod()
    {
        var controller = CreateController();
        var request = CreateRequest("unknown/method");

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.NotNull(response.Error);
        Assert.Equal(-32601, response.Error!.Code);
    }

    // --- Response ID propagation ---

    [Fact]
    public async Task HandleRequest_PropagatesId()
    {
        var controller = CreateController();
        var request = CreateRequest("initialize", id: 42);

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        // Id passes through as-is from the request
        Assert.NotNull(response.Id);
    }

    [Fact]
    public async Task HandleRequest_PropagatesId_OnError()
    {
        var controller = CreateController();
        var request = CreateRequest("unknown/method", id: 99);

        var result = await controller.HandleRequest(request);
        var response = GetResponse(result);

        Assert.NotNull(response.Id);
    }
}

public class McpMethodNotFoundExceptionTest
{
    [Fact]
    public void Constructor_SetsMessage()
    {
        var ex = new McpMethodNotFoundException("test message");
        Assert.Equal("test message", ex.Message);
    }
}

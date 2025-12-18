using Microsoft.AspNetCore.Mvc;
using Site.Models.Mcp;
using Site.Services;
using Core;

namespace Site.Controllers;

[Route("mcp")]
[ApiController]
public class McpController : ControllerBase
{
    private readonly IMcpDocumentationService _docService;
    private readonly IVersionInfo _versionInfo;
    private readonly ILogger<McpController> _logger;

    public McpController(
        IMcpDocumentationService docService,
        IVersionInfo versionInfo,
        ILogger<McpController> logger)
    {
        _docService = docService;
        _versionInfo = versionInfo;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleRequest([FromBody] McpRequest request)
    {
        _logger.LogInformation("MCP request: {Method}", request.Method);

        try
        {
            object? result = request.Method switch
            {
                "initialize" => await HandleInitializeAsync(request),
                "resources/list" => await HandleResourcesListAsync(request),
                "resources/read" => await HandleResourcesReadAsync(request),
                "tools/list" => await HandleToolsListAsync(request),
                "tools/call" => await HandleToolsCallAsync(request),
                _ => throw new McpMethodNotFoundException($"Method not found: {request.Method}")
            };

            return Ok(new McpResponse
            {
                Jsonrpc = "2.0",
                Result = result,
                Id = request.Id
            });
        }
        catch (McpMethodNotFoundException ex)
        {
            _logger.LogWarning(ex, "MCP method not found: {Method}", request.Method);
            return Ok(CreateErrorResponse(-32601, ex.Message, request.Id));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid MCP request parameters");
            return Ok(CreateErrorResponse(-32602, "Invalid params", request.Id, ex.Message));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            return Ok(CreateErrorResponse(-32001, "Resource not found", request.Id, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized resource access attempt");
            return Ok(CreateErrorResponse(-32002, "Access denied", request.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in MCP request");
            return Ok(CreateErrorResponse(-32603, "Internal error", request.Id));
        }
    }

    private Task<InitializeResult> HandleInitializeAsync(McpRequest request)
    {
        return Task.FromResult(new InitializeResult
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new Capabilities
            {
                Resources = new ResourcesCapability
                {
                    Subscribe = false,
                    ListChanged = false
                },
                Tools = new ToolsCapability
                {
                    ListChanged = false
                }
            },
            ServerInfo = new ServerInfo
            {
                Name = "WaterAlarm Documentation Server",
                Version = _versionInfo.Version ?? "1.0.0"
            }
        });
    }

    private async Task<ResourceListResult> HandleResourcesListAsync(McpRequest request)
    {
        var resources = await _docService.ListResourcesAsync();
        return new ResourceListResult { Resources = resources };
    }

    private Task<ResourceReadResult> HandleResourcesReadAsync(McpRequest request)
    {
        if (request.Params is not System.Text.Json.JsonElement paramsElement)
        {
            throw new ArgumentException("Invalid params format");
        }

        var uri = paramsElement.GetProperty("uri").GetString();
        if (string.IsNullOrEmpty(uri))
        {
            throw new ArgumentException("Missing 'uri' parameter");
        }

        var content = _docService.ReadResourceAsync(uri).Result;
        return Task.FromResult(new ResourceReadResult
        {
            Contents = new List<ResourceContent> { content }
        });
    }

    private Task<ToolListResult> HandleToolsListAsync(McpRequest request)
    {
        var tools = new List<Tool>
        {
            new Tool
            {
                Name = "search_documentation",
                Description = "Search WaterAlarm documentation for information about setup, configuration, sensors, integrations, troubleshooting, and more. Returns relevant documentation sections.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new
                        {
                            type = "string",
                            description = "Search query (e.g., 'LoRaWAN setup', 'NB-IoT troubleshooting', 'Home Assistant integration', 'sensor configuration')"
                        }
                    },
                    required = new[] { "query" }
                }
            }
        };

        return Task.FromResult(new ToolListResult { Tools = tools });
    }

    private async Task<ToolCallResult> HandleToolsCallAsync(McpRequest request)
    {
        if (request.Params is not System.Text.Json.JsonElement paramsElement)
        {
            throw new ArgumentException("Invalid params format");
        }

        var name = paramsElement.GetProperty("name").GetString();
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Missing 'name' parameter");
        }

        if (name != "search_documentation")
        {
            throw new ArgumentException($"Unknown tool: {name}");
        }

        // Extract arguments
        var hasArguments = paramsElement.TryGetProperty("arguments", out var argumentsElement);
        if (!hasArguments || !argumentsElement.TryGetProperty("query", out var queryElement))
        {
            throw new ArgumentException("Missing 'query' argument");
        }

        var query = queryElement.GetString();
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be empty");
        }

        // Execute search
        var content = await _docService.SearchDocumentationAsync(query);
        
        return new ToolCallResult
        {
            Content = content,
            IsError = false
        };
    }

    private static McpResponse CreateErrorResponse(int code, string message, object? id, object? data = null)
    {
        return new McpResponse
        {
            Jsonrpc = "2.0",
            Error = new McpError
            {
                Code = code,
                Message = message,
                Data = data
            },
            Id = id
        };
    }
}

public class McpMethodNotFoundException : Exception
{
    public McpMethodNotFoundException(string message) : base(message) { }
}

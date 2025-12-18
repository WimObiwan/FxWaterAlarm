using Site.Models.Mcp;

namespace Site.Services;

public interface IMcpDocumentationService
{
    Task<List<Resource>> ListResourcesAsync();
    Task<ResourceContent> ReadResourceAsync(string uri);
    Task<List<ToolContent>> SearchDocumentationAsync(string query);
}

public class McpDocumentationService : IMcpDocumentationService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<McpDocumentationService> _logger;
    private const string DocsBasePath = "wwwroot/Docs";
    private const string UriPrefix = "docs://";

    public McpDocumentationService(
        IWebHostEnvironment environment,
        ILogger<McpDocumentationService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public Task<List<Resource>> ListResourcesAsync()
    {
        var resources = new List<Resource>();
        var docsPath = Path.Combine(_environment.ContentRootPath, DocsBasePath);

        _logger.LogInformation("Looking for documentation in: {Path}", docsPath);
        _logger.LogInformation("ContentRootPath: {ContentRoot}", _environment.ContentRootPath);
        _logger.LogInformation("Directory exists: {Exists}", Directory.Exists(docsPath));

        if (!Directory.Exists(docsPath))
        {
            _logger.LogWarning("Documentation directory not found: {Path}", docsPath);
            return Task.FromResult(resources);
        }

        var markdownFiles = Directory.GetFiles(docsPath, "*.md", SearchOption.AllDirectories);
        _logger.LogInformation("Found {Count} markdown files", markdownFiles.Length);

        foreach (var filePath in markdownFiles)
        {
            var relativePath = Path.GetRelativePath(docsPath, filePath)
                .Replace('\\', '/');
            
            var uri = $"{UriPrefix}{relativePath}";
            var name = Path.GetFileNameWithoutExtension(filePath);
            var description = GetFileDescription(relativePath);

            resources.Add(new Resource
            {
                Uri = uri,
                Name = name,
                Description = description,
                MimeType = "text/markdown"
            });
        }

        _logger.LogInformation("Listed {Count} documentation resources", resources.Count);
        return Task.FromResult(resources);
    }

    public async Task<ResourceContent> ReadResourceAsync(string uri)
    {
        if (!uri.StartsWith(UriPrefix))
        {
            throw new ArgumentException($"Invalid URI scheme. Expected '{UriPrefix}'", nameof(uri));
        }

        var relativePath = uri.Substring(UriPrefix.Length);
        var docsPath = Path.Combine(_environment.ContentRootPath, DocsBasePath);
        var filePath = Path.Combine(docsPath, relativePath);

        // Security: ensure path is within docs directory
        var fullDocsPath = Path.GetFullPath(docsPath);
        var fullFilePath = Path.GetFullPath(filePath);
        
        if (!fullFilePath.StartsWith(fullDocsPath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Path traversal attempt detected: {Uri}", uri);
            throw new UnauthorizedAccessException("Access denied to resource outside documentation directory");
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Resource not found: {Uri}", uri);
            throw new FileNotFoundException($"Resource not found: {uri}");
        }

        var content = await File.ReadAllTextAsync(filePath);

        _logger.LogDebug("Read resource: {Uri} ({Length} bytes)", uri, content.Length);
        return new ResourceContent
        {
            Uri = uri,
            MimeType = "text/markdown",
            Text = content
        };
    }

    private string GetFileDescription(string relativePath)
    {
        // Generate human-readable descriptions based on path
        var parts = relativePath.Split('/');
        if (parts.Length == 1)
        {
            return $"Documentation: {Path.GetFileNameWithoutExtension(parts[0])}";
        }

        var category = parts[0] switch
        {
            "_Admin" => "Administration",
            "Integraties" => "Integrations",
            "Sensor_Nodes" => "Sensor Nodes",
            "3D-designs" => "3D Designs",
            _ => parts[0]
        };

        var fileName = Path.GetFileNameWithoutExtension(parts[^1]);
        return $"{category}: {fileName}";
    }

    public async Task<List<ToolContent>> SearchDocumentationAsync(string query)
    {
        var results = new List<ToolContent>();
        var docsPath = Path.Combine(_environment.ContentRootPath, DocsBasePath);

        if (!Directory.Exists(docsPath))
        {
            _logger.LogWarning("Documentation directory not found: {Path}", docsPath);
            return results;
        }

        var queryLower = query.ToLowerInvariant();
        var markdownFiles = Directory.GetFiles(docsPath, "*.md", SearchOption.AllDirectories);
        var matchedFiles = new List<(string filePath, string relativePath, int score)>();

        // Score each file based on matches
        foreach (var filePath in markdownFiles)
        {
            var content = await File.ReadAllTextAsync(filePath);
            var contentLower = content.ToLowerInvariant();
            var relativePath = Path.GetRelativePath(docsPath, filePath).Replace('\\', '/');
            
            // Calculate relevance score
            int score = 0;
            
            // Exact phrase match in content
            if (contentLower.Contains(queryLower))
            {
                score += 100;
                // Count occurrences
                score += CountOccurrences(contentLower, queryLower) * 10;
            }
            
            // Match individual words
            var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in queryWords)
            {
                var wordLower = word.ToLowerInvariant();
                if (wordLower.Length < 3) continue; // Skip very short words
                
                if (contentLower.Contains(wordLower))
                {
                    score += 10;
                }
                
                // Boost score if in filename or path
                if (relativePath.ToLowerInvariant().Contains(wordLower))
                {
                    score += 50;
                }
            }

            if (score > 0)
            {
                matchedFiles.Add((filePath, relativePath, score));
            }
        }

        // Sort by score descending and take top 5
        var topMatches = matchedFiles
            .OrderByDescending(m => m.score)
            .Take(5)
            .ToList();

        if (topMatches.Count == 0)
        {
            results.Add(new ToolContent
            {
                Type = "text",
                Text = $"No documentation found matching '{query}'. Try different search terms or browse all documentation using resources/list."
            });
            return results;
        }

        // Build response with matched content
        var response = new System.Text.StringBuilder();
        response.AppendLine($"Found {topMatches.Count} relevant documentation file(s):\n");

        foreach (var (filePath, relativePath, score) in topMatches)
        {
            var content = await File.ReadAllTextAsync(filePath);
            var description = GetFileDescription(relativePath);
            
            response.AppendLine($"## {description}");
            response.AppendLine($"**File:** `{relativePath}` (relevance: {score})");
            response.AppendLine();
            
            // Extract relevant section (first 1000 chars or up to first heading after match)
            var excerpt = ExtractRelevantSection(content, query, 1500);
            response.AppendLine(excerpt);
            response.AppendLine();
            response.AppendLine("---");
            response.AppendLine();
        }

        results.Add(new ToolContent
        {
            Type = "text",
            Text = response.ToString()
        });

        _logger.LogInformation("Search for '{Query}' returned {Count} results", query, topMatches.Count);
        return results;
    }

    private static int CountOccurrences(string text, string search)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(search, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += search.Length;
        }
        return count;
    }

    private static string ExtractRelevantSection(string content, string query, int maxLength)
    {
        var queryLower = query.ToLowerInvariant();
        var contentLower = content.ToLowerInvariant();
        
        // Find first occurrence of query
        int index = contentLower.IndexOf(queryLower, StringComparison.Ordinal);
        
        if (index == -1)
        {
            // If exact phrase not found, just return from beginning
            return content.Length > maxLength 
                ? content.Substring(0, maxLength) + "\n\n*(truncated...)*" 
                : content;
        }

        // Start from beginning of line containing match
        int startIndex = content.LastIndexOf('\n', Math.Max(0, index - 200));
        if (startIndex == -1) startIndex = 0;
        
        // Extract section
        int endIndex = Math.Min(content.Length, startIndex + maxLength);
        var excerpt = content.Substring(startIndex, endIndex - startIndex).Trim();
        
        if (endIndex < content.Length)
        {
            excerpt += "\n\n*(truncated...)*";
        }
        
        return excerpt;
    }
}


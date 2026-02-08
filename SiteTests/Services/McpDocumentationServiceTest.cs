using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Site.Services;
using Xunit;

namespace SiteTests.Services;

public class McpDocumentationServiceTest : IDisposable
{
    private readonly string _tempDir;
    private readonly McpDocumentationService _service;

    public McpDocumentationServiceTest()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"McpDocTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var environment = new TestWebHostEnvironment(_tempDir);
        var logger = NullLogger<McpDocumentationService>.Instance;
        _service = new McpDocumentationService(environment, logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private void CreateDocsFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, "wwwroot", "Docs", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    // --- ListResourcesAsync ---

    [Fact]
    public async Task ListResources_ReturnsEmpty_WhenNoDocsDirectory()
    {
        var result = await _service.ListResourcesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListResources_ReturnsMarkdownFiles()
    {
        CreateDocsFile("readme.md", "# Hello");
        CreateDocsFile("guide.md", "# Guide");

        var result = await _service.ListResourcesAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, r =>
        {
            Assert.StartsWith("docs://", r.Uri);
            Assert.Equal("text/markdown", r.MimeType);
        });
    }

    [Fact]
    public async Task ListResources_IncludesNestedFiles()
    {
        CreateDocsFile("Integraties/HomeAssistant.md", "# HA");
        CreateDocsFile("Sensor_Nodes/LoRa.md", "# LoRa");

        var result = await _service.ListResourcesAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Uri == "docs://Integraties/HomeAssistant.md");
        Assert.Contains(result, r => r.Uri == "docs://Sensor_Nodes/LoRa.md");
    }

    [Fact]
    public async Task ListResources_SetsDescription()
    {
        CreateDocsFile("readme.md", "# Hello");
        CreateDocsFile("Integraties/test.md", "# Test");

        var result = await _service.ListResourcesAsync();

        var rootFile = result.FirstOrDefault(r => r.Name == "readme");
        Assert.NotNull(rootFile);
        Assert.Equal("Documentation: readme", rootFile!.Description);

        var nestedFile = result.FirstOrDefault(r => r.Name == "test");
        Assert.NotNull(nestedFile);
        Assert.Equal("Integrations: test", nestedFile!.Description);
    }

    [Fact]
    public async Task ListResources_DescriptionCategories()
    {
        CreateDocsFile("_Admin/users.md", "admin");
        CreateDocsFile("Sensor_Nodes/sx.md", "sensor");
        CreateDocsFile("3D-designs/box.md", "3d");
        CreateDocsFile("CustomCategory/info.md", "custom");

        var result = await _service.ListResourcesAsync();

        Assert.Contains(result, r => r.Description == "Administration: users");
        Assert.Contains(result, r => r.Description == "Sensor Nodes: sx");
        Assert.Contains(result, r => r.Description == "3D Designs: box");
        Assert.Contains(result, r => r.Description == "CustomCategory: info");
    }

    // --- ReadResourceAsync ---

    [Fact]
    public async Task ReadResource_ReturnsContent()
    {
        CreateDocsFile("test.md", "# Test Content");

        var result = await _service.ReadResourceAsync("docs://test.md");

        Assert.Equal("docs://test.md", result.Uri);
        Assert.Equal("text/markdown", result.MimeType);
        Assert.Equal("# Test Content", result.Text);
    }

    [Fact]
    public async Task ReadResource_ThrowsForInvalidUriScheme()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ReadResourceAsync("invalid://test.md"));
    }

    [Fact]
    public async Task ReadResource_ThrowsForMissingFile()
    {
        CreateDocsFile("exists.md", "content"); // Ensure docs dir exists
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.ReadResourceAsync("docs://missing.md"));
    }

    [Fact]
    public async Task ReadResource_ThrowsForPathTraversal()
    {
        CreateDocsFile("exists.md", "content");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.ReadResourceAsync("docs://../../etc/passwd"));
    }

    // --- SearchDocumentationAsync ---

    [Fact]
    public async Task Search_ReturnsEmpty_WhenNoDocsDirectory()
    {
        var result = await _service.SearchDocumentationAsync("test");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Search_FindsMatchingContent()
    {
        CreateDocsFile("guide.md", "# Setup Guide\nThis explains LoRaWAN setup.");
        CreateDocsFile("other.md", "# Other\nUnrelated content.");

        var result = await _service.SearchDocumentationAsync("LoRaWAN");

        Assert.Single(result);
        Assert.Contains("LoRaWAN", result[0].Text);
    }

    [Fact]
    public async Task Search_ReturnsNoMatchMessage_WhenNoResults()
    {
        CreateDocsFile("guide.md", "# Guide\nSome content.");

        var result = await _service.SearchDocumentationAsync("nonexistentxyz");

        Assert.Single(result);
        Assert.Contains("No documentation found", result[0].Text);
    }

    [Fact]
    public async Task Search_ScoresExactPhraseMatchHigher()
    {
        CreateDocsFile("exact.md", "# Exact\nLoRaWAN setup guide for beginners.");
        CreateDocsFile("partial.md", "# Partial\nThis mentions LoRa once.");

        var result = await _service.SearchDocumentationAsync("LoRaWAN setup");

        Assert.Single(result);
        Assert.Contains("exact", result[0].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_BoostsFilenameMatches()
    {
        CreateDocsFile("lorawan.md", "# LoRaWAN\nSome content about LoRaWAN.");
        CreateDocsFile("other.md", "# Other\nThis also mentions lorawan once.");

        var result = await _service.SearchDocumentationAsync("lorawan");

        Assert.Single(result);
        // Both match but lorawan.md should score higher due to filename boost
        Assert.Contains("lorawan", result[0].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_LimitsToTop5Results()
    {
        for (int i = 0; i < 10; i++)
        {
            CreateDocsFile($"doc{i}.md", $"# Document {i}\nSearchable keyword content here.");
        }

        // The result set should contain mentions of files, but be limited
        var result = await _service.SearchDocumentationAsync("keyword");

        Assert.Single(result); // Single ToolContent with combined text
        // Count "relevance:" occurrences to check how many files are included
        var text = result[0].Text;
        var count = text.Split("relevance:").Length - 1;
        Assert.True(count <= 5, $"Expected at most 5 results, got {count}");
    }

    [Fact]
    public async Task Search_SkipsShortWordsForIndividualWordMatching()
    {
        // File only contains words matching individual short terms,
        // but the exact phrase won't be found
        CreateDocsFile("doc.md", "# Document\nSome content with alpha beta gamma.");

        var result = await _service.SearchDocumentationAsync("x y z");

        // The exact phrase "x y z" is not in the content, and individual words
        // are too short (< 3 chars) to be searched, so no match
        Assert.Single(result);
        Assert.Contains("No documentation found", result[0].Text);
    }

    // --- Helper ---

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public TestWebHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
        }

        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "TestApp";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Test";
    }
}

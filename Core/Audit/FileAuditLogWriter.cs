using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Audit;

public sealed class FileAuditLogWriter : IAuditLogWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly AuditLogOptions _options;
    private readonly ILogger<FileAuditLogWriter> _logger;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private DateOnly? _lastMaintenanceDay;

    public FileAuditLogWriter(IOptions<AuditLogOptions> options, ILogger<FileAuditLogWriter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var basePath = GetBasePath();
            Directory.CreateDirectory(basePath);

            await _mutex.WaitAsync(cancellationToken);
            try
            {
                await PerformMaintenanceIfNeededAsync(basePath, cancellationToken);

                var filePath = Path.Combine(basePath, $"audit-{DateTime.UtcNow:yyyy-MM-dd}.jsonl");
                var line = JsonSerializer.Serialize(auditEvent, JsonOptions);
                await File.AppendAllTextAsync(filePath, line + Environment.NewLine, Encoding.UTF8, cancellationToken);
            }
            finally
            {
                _mutex.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append audit event to file");
            throw;
        }
    }

    private string GetBasePath()
    {
        if (Path.IsPathRooted(_options.BasePath))
            return _options.BasePath;

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), _options.BasePath));
    }

    private async Task PerformMaintenanceIfNeededAsync(string basePath, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (_lastMaintenanceDay == today)
            return;

        _lastMaintenanceDay = today;

        foreach (var file in Directory.EnumerateFiles(basePath, "audit-*.jsonl"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = TryParseAuditDate(file);
            if (date == null)
                continue;

            var ageDays = (today.ToDateTime(TimeOnly.MinValue) - date.Value.ToDateTime(TimeOnly.MinValue)).TotalDays;
            if (ageDays > _options.RetentionDays)
            {
                File.Delete(file);
                continue;
            }

            if (ageDays > _options.CompressAfterDays)
            {
                var gzipPath = file + ".gz";
                if (!File.Exists(gzipPath))
                {
                    await CompressAsync(file, gzipPath, cancellationToken);
                    File.Delete(file);
                }
            }
        }

        foreach (var file in Directory.EnumerateFiles(basePath, "audit-*.jsonl.gz"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = TryParseAuditDate(file);
            if (date == null)
                continue;

            var ageDays = (today.ToDateTime(TimeOnly.MinValue) - date.Value.ToDateTime(TimeOnly.MinValue)).TotalDays;
            if (ageDays > _options.RetentionDays)
                File.Delete(file);
        }
    }

    private static async Task CompressAsync(string inputPath, string outputPath, CancellationToken cancellationToken)
    {
        await using var source = File.OpenRead(inputPath);
        await using var destination = File.Create(outputPath);
        await using var gzip = new GZipStream(destination, CompressionLevel.Optimal);
        await source.CopyToAsync(gzip, cancellationToken);
    }

    private static DateOnly? TryParseAuditDate(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (!fileName.StartsWith("audit-", StringComparison.OrdinalIgnoreCase))
            return null;

        var datePart = fileName.Replace("audit-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".jsonl.gz", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".jsonl", string.Empty, StringComparison.OrdinalIgnoreCase);

        return DateOnly.TryParseExact(datePart, "yyyy-MM-dd", out var date) ? date : null;
    }
}

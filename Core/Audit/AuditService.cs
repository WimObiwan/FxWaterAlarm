using Microsoft.Extensions.Logging;

namespace Core.Audit;

public interface IAuditLogWriter
{
    Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}

public interface IAuditService
{
    IDisposable BeginAction(string action, AuditTarget? target = null);

    Task LogAsync(
        AuditOutcome outcome,
        AuditDetails? details = null,
        IReadOnlyList<AuditChange>? changes = null,
        AuditTarget? target = null,
        string? action = null,
        CancellationToken cancellationToken = default);
}

public sealed class AuditService : IAuditService
{
    private readonly IAuditScopeAccessor _scopeAccessor;
    private readonly IAuditLogWriter _writer;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditScopeAccessor scopeAccessor, IAuditLogWriter writer, ILogger<AuditService> logger)
    {
        _scopeAccessor = scopeAccessor;
        _writer = writer;
        _logger = logger;
    }

    public IDisposable BeginAction(string action, AuditTarget? target = null)
    {
        return _scopeAccessor.PushAction(action, target);
    }

    public async Task LogAsync(
        AuditOutcome outcome,
        AuditDetails? details = null,
        IReadOnlyList<AuditChange>? changes = null,
        AuditTarget? target = null,
        string? action = null,
        CancellationToken cancellationToken = default)
    {
        var current = _scopeAccessor.Current;
        var resolvedAction = action ?? current?.Action;

        if (string.IsNullOrWhiteSpace(resolvedAction))
            return;

        var resolvedTarget = MergeTarget(current?.Target, target);

        var auditEvent = new AuditEvent
        {
            TimestampUtc = DateTime.UtcNow,
            CorrelationId = current?.CorrelationId ?? Guid.NewGuid().ToString(),
            Outcome = outcome,
            Action = resolvedAction,
            Actor = current?.Actor ?? new AuditActor
            {
                Identity = "unknown",
                AuthType = "Anonymous",
                IsAdmin = false
            },
            Client = current?.Client ?? new AuditClient
            {
                IpAddress = "unknown"
            },
            Target = resolvedTarget ?? new AuditTarget(),
            Changes = changes,
            Details = details
        };

        try
        {
            await _writer.WriteAsync(auditEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit write failed for action {Action}", resolvedAction);
        }
    }

    private static AuditTarget? MergeTarget(AuditTarget? left, AuditTarget? right)
    {
        if (left == null && right == null)
            return null;

        return new AuditTarget
        {
            AccountUid = right?.AccountUid ?? left?.AccountUid,
            AccountLink = right?.AccountLink ?? left?.AccountLink,
            SensorUid = right?.SensorUid ?? left?.SensorUid,
            SensorLink = right?.SensorLink ?? left?.SensorLink,
            DevEui = right?.DevEui ?? left?.DevEui
        };
    }
}

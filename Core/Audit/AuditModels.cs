namespace Core.Audit;

public enum AuditOutcome
{
    Attempted,
    Succeeded,
    Denied,
    Failed
}

public sealed class AuditActor
{
    public required string Identity { get; init; }
    public required string AuthType { get; init; }
    public required bool IsAdmin { get; init; }
}

public sealed class AuditClient
{
    public required string IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? RequestPath { get; init; }
}

public sealed class AuditTarget
{
    public Guid? AccountUid { get; init; }
    public string? AccountLink { get; init; }
    public Guid? SensorUid { get; init; }
    public string? SensorLink { get; init; }
    public string? DevEui { get; init; }
}

public sealed class AuditChange
{
    public required string Entity { get; init; }
    public required Dictionary<string, object?> Key { get; init; }
    public required string Property { get; init; }
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}

public sealed class AuditDetails
{
    public string? Reason { get; init; }
    public string? Message { get; init; }
    public string? ExceptionType { get; init; }
    public IReadOnlyList<string>? ValidationErrors { get; init; }
}

public sealed class AuditEvent
{
    public required DateTime TimestampUtc { get; init; }
    public required string CorrelationId { get; init; }
    public required AuditOutcome Outcome { get; init; }
    public required string Action { get; init; }
    public required AuditActor Actor { get; init; }
    public required AuditClient Client { get; init; }
    public required AuditTarget Target { get; init; }
    public IReadOnlyList<AuditChange>? Changes { get; init; }
    public AuditDetails? Details { get; init; }
}

public sealed class AuditScopeContext
{
    public required string CorrelationId { get; init; }
    public required AuditActor Actor { get; init; }
    public required AuditClient Client { get; init; }
    public string? Action { get; init; }
    public AuditTarget? Target { get; init; }
}

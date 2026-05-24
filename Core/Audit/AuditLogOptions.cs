namespace Core.Audit;

public sealed class AuditLogOptions
{
    public const string Location = "AuditLog";

    public string BasePath { get; set; } = "logs/audit";
    public int RetentionDays { get; set; } = 365;
    public int CompressAfterDays { get; set; } = 30;
}

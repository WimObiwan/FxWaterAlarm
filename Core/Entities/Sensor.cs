namespace Core.Entities;

public enum SensorType
{ 
    Level = 0, 
    Detect = 1,
    Moisture = 2
}

public class Sensor
{
    private readonly List<Account> _accounts = null!;

    private readonly List<AccountSensor> _accountSensors = null!;
    public int Id { get; } = 0;
    public required Guid Uid { get; init; }
    public required string DevEui { get; init; }
    public required DateTime CreateTimestamp { get; init; }
    public required SensorType Type { get; init; }
    public string? Link { get; set; }
    public IReadOnlyCollection<AccountSensor> AccountSensors => _accountSensors.AsReadOnly();
    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();

    public bool SupportsCapacity => Type == SensorType.Level;
    public bool SupportsDistance => Type == SensorType.Level;
    public bool SupportsPercentage => Type == SensorType.Level || Type == SensorType.Moisture;
    public bool SupportsGraph => Type == SensorType.Level || Type == SensorType.Moisture;
    public bool SupportsMinMaxConstraints => Type == SensorType.Level;
    public bool SupportsTrend => Type == SensorType.Level;
    public bool SupportsAlerts => Type == SensorType.Level || Type == SensorType.Detect;
}
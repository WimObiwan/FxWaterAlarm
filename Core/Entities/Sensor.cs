namespace Core.Entities;

public enum SensorType
{ 
    Level = 0, 
    Detect = 1
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
}
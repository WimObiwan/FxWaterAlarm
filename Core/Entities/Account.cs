namespace Core.Entities;

public class Account
{
    private readonly List<AccountSensor> _accountSensors = null!;
    private readonly List<Sensor> _sensors = null!;
    public int Id { get; init; }
    public required Guid Uid { get; init; }
    public required string Email { get; set; }
    public string? Name { get; set; }
    public required DateTime CreationTimestamp { get; init; }
    public string? Link { get; set; }
    public IReadOnlyCollection<AccountSensor> AccountSensors => _accountSensors?.AsReadOnly()!;
    public IReadOnlyCollection<Sensor> Sensors => _sensors.AsReadOnly();
    
    public string? RestPath => Link != null ? $"/a/{Link}" : null;

    public bool IsDemo => IsDemoEmail(Email);

    public string? AppPath
    {
        get
        {
            if (IsDemo)
                return null;
            if (_accountSensors.Count == 1)
                return _accountSensors.Single().RestPath;
            return RestPath;
        }
    }

    public static bool IsDemoEmail(string email) =>
        string.Equals(email, "demo@wateralarm.be", StringComparison.InvariantCultureIgnoreCase);
    
    public void AddSensor(Sensor sensor)
    {
        _accountSensors.Add(
            new AccountSensor
            {
                Account = this,
                Sensor = sensor,
                CreateTimestamp = DateTime.UtcNow
            });
    }

    public bool RemoveSensor(Sensor sensor)
    {
        return _sensors.Remove(sensor);
    }
}
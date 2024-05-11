namespace Core.Entities;

public class AccountSensor
{
    private readonly List<AccountSensorAlarm> _alarms = null!;
    public required Account Account { get; init; }
    public required Sensor Sensor { get; init; }
    public required DateTime CreateTimestamp { get; init; }

    public string? Name { get; set; }
    public int? DistanceMmEmpty { get; set; }
    public int? DistanceMmFull { get; set; }
    public int? CapacityL { get; set; }
    public bool AlertsEnabled { get; set; }
    public IReadOnlyCollection<AccountSensorAlarm> Alarms => _alarms?.AsReadOnly()!;

    public double? ResolutionL
    {
        get
        {
            if (!DistanceMmEmpty.HasValue || !DistanceMmFull.HasValue || !CapacityL.HasValue)
                return null;

            return 1.0 / (DistanceMmEmpty.Value - DistanceMmFull.Value) * CapacityL.Value;
        }
    }

    public string? RestPath =>
        Account.Link != null && Sensor.Link != null ? $"/a/{Account.Link}/s/{Sensor.Link}" : null;

    public void AddAlarm(AccountSensorAlarm alarm)
    {
        _alarms.Add(alarm);
    }

    public bool RemoveSensor(AccountSensorAlarm alarm)
    {
        return _alarms.Remove(alarm);
    }
}
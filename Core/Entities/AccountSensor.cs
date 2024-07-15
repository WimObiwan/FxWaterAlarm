namespace Core.Entities;

public enum GraphType
{
    None,
    Height,
    Percentage,
    Capacity,
    RssiDbm,
    BatV,
}
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
    public bool NoMinMaxConstraints { get; set; }
    
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

    public bool HasHeight => 
        DistanceMmEmpty.HasValue
        && DistanceMmEmpty.Value > 0;

    public bool HasPercentage => 
        HasHeight
        && DistanceMmEmpty.HasValue
        && DistanceMmFull.HasValue 
        && DistanceMmFull.Value < DistanceMmEmpty.Value;

    public bool HasCapacity => 
        HasPercentage
        && CapacityL.HasValue
        && CapacityL > 0;

    public GraphType GraphType
    {
        get
        {
            if (HasCapacity)
                return GraphType.Capacity;
            else if (HasPercentage)
                return GraphType.Percentage;
            else if (HasHeight)
                return GraphType.Height;
            else
                return GraphType.None;
        }
    }

    public void AddAlarm(AccountSensorAlarm alarm)
    {
        _alarms.Add(alarm);
    }

    public bool RemoveSensor(AccountSensorAlarm alarm)
    {
        return _alarms.Remove(alarm);
    }
}
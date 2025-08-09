using Core.Exceptions;

namespace Core.Entities;

public enum GraphType
{
    None,
    Height,
    Percentage,
    Volume,
    Distance,
    RssiDbm,
    Reception,
    BatV,
    Temperature,
    Conductivity,
    Status,
}
public class AccountSensor
{
    private readonly List<AccountSensorAlarm> _alarms = null!;
    public required Account Account { get; init; }
    public required Sensor Sensor { get; init; }
    public required DateTime CreateTimestamp { get; init; }

    public int Order { get; set; }
    public bool Disabled { get; set; }
    public string? Name { get; set; }
    public int? DistanceMmEmpty { get; set; }
    public int? DistanceMmFull { get; set; }
    public int? CapacityL { get; set; }
    public int? UnusableHeightMm { get; set; }
    public bool AlertsEnabled { get; set; }
    public bool NoMinMaxConstraints { get; set; }
    
    public IReadOnlyCollection<AccountSensorAlarm> Alarms => _alarms?.AsReadOnly()!;

    public double? ResolutionL
    {
        get
        {
            if (Sensor.Type == SensorType.Level)
            {
                if (!DistanceMmEmpty.HasValue || !DistanceMmFull.HasValue || !CapacityL.HasValue)
                    return null;

                return 1.0 / (DistanceMmEmpty.Value - DistanceMmFull.Value) * CapacityL.Value;
            }


            if (Sensor.Type == SensorType.LevelPressure)
            {
                if (!DistanceMmFull.HasValue || !CapacityL.HasValue)
                    return null;

                return 1.0 / ((DistanceMmEmpty ?? 0) + DistanceMmFull.Value) * CapacityL.Value;
            }

            return null;
        }
    }

    public double? UnusableCapacityL =>
        ResolutionL.HasValue && UnusableHeightMm.HasValue ? UnusableHeightMm * ResolutionL : null;

    public double? UsableCapacityL =>
        CapacityL.HasValue && ResolutionL.HasValue ? (CapacityL.Value - (UnusableHeightMm ?? 0) * ResolutionL) : null;

    public string? RestPath =>
        Account.Link != null && Sensor.Link != null ? $"/a/{Account.Link}/s/{Sensor.Link}" : null;

    public string? ApiRestPath =>
        Account.Link != null && Sensor.Link != null ? $"/api/a/{Account.Link}/s/{Sensor.Link}" : null;

    public bool HasDistance =>
        Sensor.SupportsDistance;

    public bool HasHeight =>
        Sensor.SupportsHeight
        || (Sensor.SupportsDistance && DistanceMmEmpty.HasValue && DistanceMmEmpty.Value > 0);

    private bool HasPercentageUsingHeight =>
        (
            Sensor.Type == SensorType.Level
            && HasHeight
            && DistanceMmEmpty.HasValue
            && DistanceMmFull.HasValue
            && DistanceMmFull.Value < DistanceMmEmpty.Value
        )
        ||
        (
            Sensor.Type == SensorType.LevelPressure
            && HasHeight
            && DistanceMmFull.HasValue
        );

    public bool HasPercentage =>
        Sensor.SupportsPercentage
        && (
            Sensor.Type != SensorType.Level && Sensor.Type != SensorType.LevelPressure
            || HasPercentageUsingHeight
        );

    public bool HasVolume => 
        Sensor.SupportsCapacity
        && HasPercentage
        && CapacityL.HasValue
        && CapacityL > 0;

    public bool HasTemperature => 
        Sensor.SupportsTemperature;

    public bool HasConductivity =>
        Sensor.SupportsConductivity;

    public bool HasStatus =>
        Sensor.SupportsStatus;

    public int RoundVolume
    {
        get
        {
            if (CapacityL < 20)
                return 100;
            else if (CapacityL < 200)
                return 10;
            else
                return 1;
        }
    }

    public GraphType GraphType
    {
        get
        {
            if (HasVolume)
                return GraphType.Volume;
            else if (HasPercentage)
                return GraphType.Percentage;
            else if (HasHeight)
                return GraphType.Height;
            else
                return GraphType.None;
        }
    }

    public void EnsureEnabled()
    {
        if (Disabled)
            throw
                new AccountSensorDisabledException()
                {
                    AccountUid = Account.Uid,
                    SensorUid = Sensor.Uid
                };
    }

    public void AddAlarm(AccountSensorAlarm alarm)
    {
        _alarms.Add(alarm);
    }

    public bool RemoveAlarm(AccountSensorAlarm alarm)
    {
        return _alarms.Remove(alarm);
    }
}
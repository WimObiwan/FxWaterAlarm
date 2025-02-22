namespace Core.Entities;

public enum AccountSensorAlarmType
{
    Data = 1,
    Battery = 2,
    PercentageLow = 3,
    PercentageHigh = 4,
    // PercentageStatus = 5
    HeightLow = 6,
    HeightHigh = 7,
    // HeightStatus = 8
    DetectOn = 9,
    //DetectStatus = 10,
}

public class AccountSensorAlarm
{
    public int Id { get; init; }
    public required Guid Uid { get; init; }
    public required AccountSensorAlarmType AlarmType { get; set; }
    public double? AlarmThreshold { get; set; }
    public DateTime? LastTriggered { get; set; }
    public DateTime? LastCleared { get; set; }
    public AccountSensor AccountSensor { get; init; } = null!;

    public bool IsCurrentlyTriggered =>
        LastTriggered.HasValue
        && (!LastCleared.HasValue || LastCleared.Value < LastTriggered.Value);

    public bool IsCurrentlyCleared =>
        LastCleared.HasValue
        && (!LastTriggered.HasValue || LastTriggered.Value < LastCleared.Value);
}
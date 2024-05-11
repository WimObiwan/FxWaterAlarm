namespace Core.Entities;

public enum AccountSensorAlarmType { Data = 1, Battery = 2, LevelFractionLow = 3, LevelFractionHigh = 4, LevelFractionStatus = 5 }

public class AccountSensorAlarm
{
    public int Id { get; init; }
    public required Guid Uid { get; init; }
    public required AccountSensorAlarmType AlarmType { get; set; }
    public double? AlarmThreshold { get; set; }
    // public DateTime? LastTriggered { get; set; }
    // public DateTime? LastCleared { get; set; }
    public AccountSensor AccountSensor { get; init; } = null!;
}
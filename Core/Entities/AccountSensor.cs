namespace Core.Entities;

public class AccountSensor
{
    public required Account Account { get; init; }
    public required Sensor Sensor { get; init; }
    public required DateTime CreateTimestamp { get; init; }
}
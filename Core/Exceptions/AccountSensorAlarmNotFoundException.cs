namespace Core.Exceptions;

[Serializable]
public class AccountSensorAlarmNotFoundException : Exception
{
    public AccountSensorAlarmNotFoundException()
    {
    }

    public AccountSensorAlarmNotFoundException(string message)
        : base(message)
    {
    }

    public AccountSensorAlarmNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public Guid AccountUid { get; init; }
    public Guid SensorUid { get; init; }
    public Guid AlarmUid { get; init; }
}
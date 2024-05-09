namespace Core.Exceptions;

[Serializable]
public class AccountSensorNotFoundException : Exception
{
    public AccountSensorNotFoundException()
    {
    }

    public AccountSensorNotFoundException(string message)
        : base(message)
    {
    }

    public AccountSensorNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public Guid AccountUid { get; init; }
    public Guid SensorUid { get; init; }
}
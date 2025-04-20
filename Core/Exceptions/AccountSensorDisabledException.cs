namespace Core.Exceptions;

[Serializable]
public class AccountSensorDisabledException : Exception
{
    public AccountSensorDisabledException()
    {
    }

    public AccountSensorDisabledException(string message)
        : base(message)
    {
    }

    public AccountSensorDisabledException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public Guid AccountUid { get; init; }
    public Guid SensorUid { get; init; }
}
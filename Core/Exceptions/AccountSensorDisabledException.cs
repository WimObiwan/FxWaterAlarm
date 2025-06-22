namespace Core.Exceptions;

[Serializable]
public class AccountSensorDisabledException : Exception
{
    public AccountSensorDisabledException()
    {
    }

    public AccountSensorDisabledException(Exception innerException)
        : base(message: null, innerException)
    {
    }

    public override string Message => $"Account sensor is disabled (AccountUid: {AccountUid}, SensorUid: {SensorUid})";

    public Guid AccountUid { get; init; }
    public Guid SensorUid { get; init; }
}
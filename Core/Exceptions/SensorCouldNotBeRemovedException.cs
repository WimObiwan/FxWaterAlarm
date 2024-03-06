namespace Core.Exceptions;

[Serializable]
public class SensorCouldNotBeRemovedException : Exception
{
    public SensorCouldNotBeRemovedException()
    {
    }

    public SensorCouldNotBeRemovedException(string message)
        : base(message)
    {
    }

    public SensorCouldNotBeRemovedException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public Guid? AccountUid { get; init; }
    public Guid SensorUid { get; init; }
}
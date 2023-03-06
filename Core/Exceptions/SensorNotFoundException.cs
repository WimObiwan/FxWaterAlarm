namespace Core.Exceptions;

[Serializable]
public class SensorNotFoundException : Exception
{
    public SensorNotFoundException()
    {
    }

    public SensorNotFoundException(string message)
        : base(message)
    {
    }

    public SensorNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public Guid Uid { get; init; }
}
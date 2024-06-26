namespace Core.Exceptions;

[Serializable]
public class AccountNotFoundException : Exception
{
    public AccountNotFoundException()
    {
    }

    public AccountNotFoundException(string message)
        : base(message)
    {
    }

    public AccountNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public Guid? AccountUid { get; init; }
    public string? Email { get; init; }
}
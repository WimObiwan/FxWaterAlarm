namespace Core.Util;

public readonly record struct Optional<T>(bool Specified, T? Value);

public static class Optional
{
    public static Optional<T> From<T>(T? value, T nullValue)
    {
        if (value == null)
            return new Optional<T>(false, default);
        if (value.Equals(nullValue))
            return new Optional<T>(true, default);
        return new Optional<T>(true, value);
    }

    public static Optional<string> From(string? value)
    {
        return From(value, "");
    }

    public static Optional<T?> From<T>(T? value) where T : struct
    {
        return From(value, default(T));
    }
}
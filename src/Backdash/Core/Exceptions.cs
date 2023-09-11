namespace Backdash.Core;

/// <inheritdoc />
[Serializable]
public class BackdashException : Exception
{
    internal BackdashException() { }

    internal BackdashException(string message) : base(message) { }

    internal BackdashException(string message, Exception innerException) : base(message, innerException) { }

    internal BackdashException(LogStringHandler builder) : base(builder.GetFormattedText()) { }

    internal BackdashException(LogStringHandler builder, Exception innerException)
        : base(builder.GetFormattedText(), innerException) { }
}

public class InvalidTypeArgumentException : BackdashException
{
    internal InvalidTypeArgumentException(Type type) : base($"Invalid usage of type {type.FullName}") { }

    internal InvalidTypeArgumentException(Type type, string message) : base($"{type.FullName}: {message}") { }
}

public sealed class InvalidTypeArgumentException<T> : InvalidTypeArgumentException
{
    internal InvalidTypeArgumentException() : base(typeof(T)) { }

    internal InvalidTypeArgumentException(string message) : base(typeof(T), message) { }
}

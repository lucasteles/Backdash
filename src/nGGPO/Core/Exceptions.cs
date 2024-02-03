namespace nGGPO.Core;

/// <inheritdoc />
[Serializable]
public class NggpoException : Exception
{
    internal NggpoException() { }

    internal NggpoException(string message) : base(message) { }

    internal NggpoException(string message, Exception innerException) : base(message, innerException) { }

    internal NggpoException(LogStringHandler builder) : base(builder.GetFormattedText()) { }

    internal NggpoException(LogStringHandler builder, Exception innerException)
        : base(builder.GetFormattedText(), innerException) { }
}

public sealed class InvalidTypeArgumentException : NggpoException
{
    internal InvalidTypeArgumentException(Type type) : base($"Invalid usage of type {type.FullName}") { }

    internal static InvalidTypeArgumentException For<TArg>() => new(typeof(TArg));
}

namespace Backdash.Core;

[Serializable]
public class BackdashException : Exception
{
    internal BackdashException() { }

    internal BackdashException(string message) : base(message) { }

    internal BackdashException(string message, Exception innerException) : base(message, innerException) { }
}

[Serializable]
public class InvalidTypeArgumentException : BackdashException
{
    internal InvalidTypeArgumentException(Type type) : base($"Invalid usage of type {type.FullName}") { }

    internal InvalidTypeArgumentException(Type type, string message) : base($"{type.FullName}: {message}") { }
}

[Serializable]
public sealed class InvalidTypeArgumentException<T> : InvalidTypeArgumentException
{
    internal InvalidTypeArgumentException() : base(typeof(T)) { }

    internal InvalidTypeArgumentException(string message) : base(typeof(T), message) { }
}

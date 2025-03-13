namespace Backdash.Core;

/// <summary>
///     An exception that is thrown when an error is encountered on netcode.
/// </summary>
[Serializable]
public class NetcodeException : Exception
{
    internal NetcodeException() { }
    internal NetcodeException(string message) : base(message) { }
    internal NetcodeException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
///     An exception that is thrown when an error is encountered on netcode deserialization.
/// </summary>
[Serializable]
public class NetcodeDeserializationException : NetcodeException
{
    internal NetcodeDeserializationException(Type type, string message = "") : base(
        $"Unable to deserialize {type.FullName}. {message}")
    { }
}

/// <summary>
///     An exception that is thrown when an error is encountered on netcode deserialization for type
///     <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">Deserialization type</typeparam>
[Serializable]
public class NetcodeDeserializationException<T> : NetcodeDeserializationException
{
    internal NetcodeDeserializationException(string message = "") : base(typeof(T), message) { }
}

/// <summary>
///     An exception that is thrown when an error is encountered on netcode serialization.
/// </summary>
[Serializable]
public class NetcodeSerializationException : NetcodeException
{
    internal NetcodeSerializationException(Type type, string message = "") : base(
        $"Unable to serialize {type.FullName}. {message}")
    { }
}

/// <summary>
///     An exception that is thrown when an error is encountered on netcode serialization for type
///     <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">Serialization type</typeparam>
[Serializable]
public class NetcodeSerializationException<T> : NetcodeSerializationException
{
    internal NetcodeSerializationException(string message = "") : base(typeof(T), message) { }
}

/// <summary>
///     An exception that is thrown for invalid type arguments.
/// </summary>
[Serializable]
public class InvalidTypeArgumentException : NetcodeException
{
    internal InvalidTypeArgumentException(Type type) : base($"Invalid usage of type {type.FullName}") { }
    internal InvalidTypeArgumentException(Type type, string message) : base($"{type.FullName}: {message}") { }
}

/// <summary>
///     An exception that is thrown when for invalid type argument <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">Generic type argument</typeparam>
[Serializable]
public sealed class InvalidTypeArgumentException<T> : InvalidTypeArgumentException
{
    internal InvalidTypeArgumentException() : base(typeof(T)) { }
    internal InvalidTypeArgumentException(string message) : base(typeof(T), message) { }
}

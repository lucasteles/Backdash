using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Backdash.Core;

static class TypeHelpers
{
    public static T? Instantiate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(bool allowPrivateConstructor = true) where T : notnull
    {
        var type = typeof(T);
        if (type.IsValueType)
            return default;
        var flags = BindingFlags.Instance | BindingFlags.Public;
        var ctor = type.GetConstructor(flags, null, Type.EmptyTypes, null);
        if (ctor is not null)
            return (T)ctor.Invoke([]);
        return default;
    }

    public static T? InstantiateWithNonPublicConstructor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(bool allowPrivateConstructor = true) where T : notnull
    {
        var type = typeof(T);
        if (type.IsValueType)
            return default;
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        var ctor = type.GetConstructor(flags, null, Type.EmptyTypes, null);
        if (ctor is not null)
            return (T)ctor.Invoke([]);
        return default;
    }

    public static bool HasInvariantHashCode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>() where T : notnull
    {
        var comparer = EqualityComparer<T>.Default;
        return Instantiate<T>() is { } state1
               && Instantiate<T>() is { } state2
               && Instantiate<T>() is { } state3
               && comparer.GetHashCode(state1) == comparer.GetHashCode(state2)
               && comparer.GetHashCode(state1) == comparer.GetHashCode(state3);
    }
}

using System.Reflection;

#pragma warning disable S3011

namespace Backdash.Core;

static class TypeHelpers
{
    public static T? Instantiate<T>(bool allowPrivateConstructor = true) where T : notnull
    {
        var type = typeof(T);
        if (type.IsValueType)
            return default;

        var flags = BindingFlags.Instance | BindingFlags.Public;

        if (allowPrivateConstructor)
            flags |= BindingFlags.NonPublic;

        var ctor = type.GetConstructor(flags, null, Type.EmptyTypes, null);
        if (ctor is not null)
            return (T)ctor.Invoke([]);

        return default;
    }

    public static bool HasInvariantHashCode<T>() where T : notnull
    {
        var comparer = EqualityComparer<T>.Default;
        return Instantiate<T>() is { } state1
               && Instantiate<T>() is { } state2
               && Instantiate<T>() is { } state3
               && comparer.GetHashCode(state1) == comparer.GetHashCode(state2)
               && comparer.GetHashCode(state1) == comparer.GetHashCode(state3);
    }
}

using System.Runtime.CompilerServices;

namespace nGGPO.Utils;

public static class Tracer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(string msg, params object[] args)
    {
        // Method intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(string msg, params object[] args)
    {
        // Method intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(string msg, params object[] args)
    {
        // Method intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(Exception e, string msg, params object[] args)
    {
        // Method intentionally left empty.
    }

    public static void Error(string message)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Assert(bool condition, string? msg = null)
    {
        if (msg is not null)
            System.Diagnostics.Trace.Assert(condition, msg);
        else
            System.Diagnostics.Trace.Assert(condition);
    }
}

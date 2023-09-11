using System.Runtime.CompilerServices;

namespace Backdash.Core;

public static class Tracer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Assert(bool condition, LogStringHandler builder)
    {
        if (condition) return;
        var msg = builder.GetFormattedText();
        System.Diagnostics.Trace.Assert(condition, msg);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Assert(bool condition)
    {
        if (condition) return;
        System.Diagnostics.Trace.Assert(condition);
    }
}

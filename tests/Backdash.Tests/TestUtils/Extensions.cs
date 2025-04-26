using ObjectLayoutInspector;

namespace Backdash.Tests;

using Xunit.Abstractions;

public static class Extensions
{
    public static void PrintLayout<T>(
        this ITestOutputHelper output,
        bool recursively = true,
        bool includePaddings = true
    )
    {
        if (!Env.IsDebug()) return;
        var layout = TypeLayout.GetLayout<T>(null, includePaddings);
        output.WriteLine(layout.ToString(recursively));
    }
}

using System.Diagnostics;

namespace Backdash.Tests;

public static class Env
{
    public static bool IsInContinuousIntegration =>
        bool.TryParse(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), out var ci) && ci;

    public static bool IsDebug()
    {
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (Debugger.IsAttached)
            return true;
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}

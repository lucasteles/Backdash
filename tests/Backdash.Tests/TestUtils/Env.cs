namespace Backdash.Tests.TestUtils;

public static class Env
{
    public static bool ContinuousIntegration =>
        bool.TryParse(Environment.GetEnvironmentVariable("CI"), out var ci) && ci;
}

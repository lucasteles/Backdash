namespace Backdash.Tests.TestUtils;

[AttributeUsage(AttributeTargets.Method)]
public sealed class NonCIFactAttribute : FactAttribute
{
    public NonCIFactAttribute()
    {
        if (Env.IsInContinuousIntegration)
            Skip = "Ignored in CI";
    }
}

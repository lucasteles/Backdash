using System.Runtime.CompilerServices;
namespace Backdash.Tests;

public static class Initializer
{
    public const int DefaultSeed = 42;

    [ModuleInitializer]
    public static void Init() => Randomizer.Seed = new(DefaultSeed);
}

namespace Backdash.Tests.Utils;

[CollectionDefinition(Name, DisableParallelization = true)]
public class SerialCollectionDefinition
{
    public const string Name = "Serial";
}

using Backdash.Network.Messages;
namespace Backdash.Tests.Specs.Integration.Network;
public class ProtocolMessageTests
{
    [PropertyTest(MaxTest = 10_000)]
    internal bool SerializationAndDeserialization(ProtocolMessage message) =>
        AssertSerialization.Validate(ref message);
    [PropertyTest]
    internal bool ReadWriteBufferOffset(ProtocolMessage value) =>
        AssertSerialization.Offset(ref value);
}

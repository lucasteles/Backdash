using Backdash.Network.Messages;
using Backdash.Serialization;
using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Network.Protocol;

public class ProtocolMessageTests
{
    [PropertyTest(MaxTest = 10_000)]
    internal bool SerializationAndDeserialization(ProtocolMessage value) =>
        AssertThat.Serialization.IsValid(
            ref value,
            (ref ProtocolMessage v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref ProtocolMessage v, BinaryBufferReader r) => v.Deserialize(r)
        );
}

using nGGPO.Network;

namespace nGGPO.Tests.Specs.Network;

public class UdpMsgTests
{
    [PropertyTest]
    internal bool SerializationAndDeserialization(UdpMsg message) =>
        AssertSerialization.Validate(ref message);

    [PropertyTest]
    internal bool ReadWriteBufferOffset(UdpMsg value) =>
        AssertSerialization.Offset(ref value);
}

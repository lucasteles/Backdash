using nGGPO.Network.Messages;

namespace nGGPO.Tests.Messages;

public class MessageSerializationTests
{
    [PropertyTest]
    internal bool ConnectStatusSerialize(ConnectStatus value) =>
        AssertSerialization.Validate(ref value);

    [PropertyTest]
    internal bool ConnectStatusOffset(ConnectStatus value) => AssertSerialization.Offset(ref value);
}

using Backdash.Network.Messages;
using Backdash.Serialization;
using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Network;

public class MessageSerializationTests
{
    [PropertyTest]
    internal bool ConnectStatusSerialize(ConnectStatus value) =>
        AssertThat.Serialization.IsValid(ref value,
            (ref ConnectStatus v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref ConnectStatus v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool HeaderSerialize(Header value) =>
        AssertThat.Serialization.IsValid(ref value,
            (ref Header v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref Header v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool InputAckSerialize(InputAck value) =>
        AssertThat.Serialization.IsValid(ref value,
            (ref InputAck v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref InputAck v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool QualityReportSerialize(QualityReport value) =>
        AssertThat.Serialization.IsValid(ref value,
            (ref QualityReport v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref QualityReport v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool QualityReplySerialize(QualityReply value) =>
        AssertThat.Serialization.IsValid(ref value,
            (ref QualityReply v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref QualityReply v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool SyncReplySerialize(SyncReply value) =>
        AssertThat.Serialization.IsValid(ref value,
            (ref SyncReply v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref SyncReply v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool SyncRequestSerialize(SyncRequest value) =>
        AssertThat.Serialization.IsValid(ref value,
            (ref SyncRequest v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref SyncRequest v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool InputMsgSerialize(InputMessage value) =>
        AssertThat.Serialization.IsValid(ref value,
            (ref InputMessage v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref InputMessage v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool ConsistencyCheckRequestSerialize(ConsistencyCheckRequest value) =>
        AssertThat.Serialization.IsValid(ref value,
            (ref ConsistencyCheckRequest v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref ConsistencyCheckRequest v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool ConsistencyCheckReplySerialize(ConsistencyCheckReply value) =>
        AssertThat.Serialization.IsValid(ref value,
            (ref ConsistencyCheckReply v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref ConsistencyCheckReply v, BinaryBufferReader r) => v.Deserialize(r)
        );
}

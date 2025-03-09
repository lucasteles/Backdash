using Backdash.Network.Messages;
using Backdash.Serialization;
using Backdash.Tests.TestUtils;
using Backdash.Tests.TestUtils.Assertions;

namespace Backdash.Tests.Specs.Unit.Network;

public class MessageSerializationTests
{
    [PropertyTest]
    internal bool ConnectStatusSerialize(ConnectStatus value) =>
        AssertSerialization.Validate(ref value,
            (ref ConnectStatus v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref ConnectStatus v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool HeaderSerialize(Header value) =>
        AssertSerialization.Validate(ref value,
            (ref Header v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref Header v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool InputAckSerialize(InputAck value) =>
        AssertSerialization.Validate(ref value,
            (ref InputAck v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref InputAck v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool QualityReportSerialize(QualityReport value) =>
        AssertSerialization.Validate(ref value,
            (ref QualityReport v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref QualityReport v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool QualityReplySerialize(QualityReply value) =>
        AssertSerialization.Validate(ref value,
            (ref QualityReply v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref QualityReply v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool SyncReplySerialize(SyncReply value) =>
        AssertSerialization.Validate(ref value,
            (ref SyncReply v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref SyncReply v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool SyncRequestSerialize(SyncRequest value) =>
        AssertSerialization.Validate(ref value,
            (ref SyncRequest v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref SyncRequest v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool InputMsgSerialize(InputMessage value) =>
        AssertSerialization.Validate(ref value,
            (ref InputMessage v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref InputMessage v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool ConsistencyCheckRequestSerialize(ConsistencyCheckRequest value) =>
        AssertSerialization.Validate(ref value,
            (ref ConsistencyCheckRequest v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref ConsistencyCheckRequest v, BinaryBufferReader r) => v.Deserialize(r)
        );

    [PropertyTest]
    internal bool ConsistencyCheckReplySerialize(ConsistencyCheckReply value) =>
        AssertSerialization.Validate(ref value,
            (ref ConsistencyCheckReply v, BinaryRawBufferWriter w) => v.Serialize(w),
            (ref ConsistencyCheckReply v, BinaryBufferReader r) => v.Deserialize(r)
        );
}

using Backdash.Network.Messages;
using Backdash.Tests.TestUtils;
using Backdash.Tests.TestUtils.Assertions;

namespace Backdash.Tests.Specs.Unit.Network;

public class MessageSerializationTests
{
    [PropertyTest]
    internal bool ConnectStatusSerialize(ConnectStatus value) =>
        AssertSerialization.Validate(ref value);

    [PropertyTest]
    internal bool ConnectStatusOffset(ConnectStatus value) => AssertSerialization.Offset(ref value);

    [PropertyTest]
    internal bool HeaderSerialize(Header value) => AssertSerialization.Validate(ref value);

    [PropertyTest]
    internal bool HeaderOffset(Header value) => AssertSerialization.Offset(ref value);

    [PropertyTest]
    internal bool InputAckSerialize(InputAck value) => AssertSerialization.Validate(ref value);

    [PropertyTest]
    internal bool InputAckOffset(InputAck value) => AssertSerialization.Offset(ref value);

    [PropertyTest]
    internal bool QualityReplySerialize(QualityReply value) => AssertSerialization.Validate(ref value);

    [PropertyTest]
    internal bool QualityReplyOffset(QualityReply value) => AssertSerialization.Offset(ref value);

    [PropertyTest]
    internal bool QualityReportSerialize(QualityReport value) => AssertSerialization.Validate(ref value);

    [PropertyTest]
    internal bool QualityReportOffset(QualityReport value) => AssertSerialization.Offset(ref value);

    [PropertyTest]
    internal bool SyncReplySerialize(SyncReply value) => AssertSerialization.Validate(ref value);

    [PropertyTest]
    internal bool SyncReplyOffset(SyncReply value) => AssertSerialization.Offset(ref value);

    [PropertyTest]
    internal bool SyncRequestSerialize(SyncRequest value) => AssertSerialization.Validate(ref value);

    [PropertyTest]
    internal bool SyncRequestOffset(SyncRequest value) => AssertSerialization.Offset(ref value);

    [PropertyTest]
    internal bool InputMsgSerialize(InputMessage value) => AssertSerialization.Validate(ref value);

    [PropertyTest]
    internal bool InputMsgOffset(InputMessage value) => AssertSerialization.Offset(ref value);

    [PropertyTest]
    internal bool ConsistencyCheckRequestSerialize(ConsistencyCheckRequest value) =>
        AssertSerialization.Validate(ref value);

    [PropertyTest]
    internal bool ConsistencyCheckRequestOffset(ConsistencyCheckRequest value) => AssertSerialization.Offset(ref value);

    [PropertyTest]
    internal bool ConsistencyCheckReplySerialize(ConsistencyCheckReply value) =>
        AssertSerialization.Validate(ref value);

    [PropertyTest]
    internal bool ConsistencyCheckReplyOffset(ConsistencyCheckReply value) => AssertSerialization.Offset(ref value);
}

namespace nGGPO.Network.Messages;

enum MsgType : byte
{
    Invalid,
    SyncRequest,
    SyncReply,
    Input,
    QualityReport,
    QualityReply,
    KeepAlive,
    InputAck,
}

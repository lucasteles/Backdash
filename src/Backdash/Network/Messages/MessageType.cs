namespace Backdash.Network.Messages;
[Serializable]
enum MessageType : ushort
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

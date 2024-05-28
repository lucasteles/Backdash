namespace Backdash.Network.Messages;

[Serializable]
enum MessageType : ushort
{
    Unknown,
    SyncRequest,
    SyncReply,
    Input,
    QualityReport,
    QualityReply,
    KeepAlive,
    InputAck,
}

namespace nGGPO;

public enum EventCode : short
{
    ConnectedToPeer = 1000,
    SynchronizingWithPeer = 1001,
    SynchronizedWithPeer = 1002,
    Running = 1003,
    DisconnectedFromPeer = 1004,
    TimeSync = 1005,
    ConnectionInterrupted = 1006,
    ConnectionResumed = 1007,
}

public interface IRollbackEvent
{
    EventCode Code { get; }
}

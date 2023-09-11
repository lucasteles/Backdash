namespace nGGPO.Types;

public enum EventCode
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

public abstract class NetCodeEvent
{
    public abstract EventCode Code { get; }

    public class Connected : NetCodeEvent
    {
        public override EventCode Code => EventCode.ConnectedToPeer;
    }

    public class Synchronizing : NetCodeEvent
    {
        public override EventCode Code => EventCode.SynchronizingWithPeer;
    }

    public class Synchronized : NetCodeEvent
    {
        public override EventCode Code => EventCode.SynchronizedWithPeer;
    }

    public class Disconnected : NetCodeEvent
    {
        public override EventCode Code => EventCode.DisconnectedFromPeer;
    }

    public class TimeSync : NetCodeEvent
    {
        public override EventCode Code => EventCode.TimeSync;
    }

    public class ConnectionInterrupted : NetCodeEvent
    {
        public override EventCode Code => EventCode.ConnectionInterrupted;
    }

    public class ConnectionResumed : NetCodeEvent
    {
        public override EventCode Code => EventCode.ConnectionResumed;
    }
}
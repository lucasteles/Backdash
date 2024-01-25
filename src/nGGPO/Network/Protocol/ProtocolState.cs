namespace nGGPO.Network.Protocol;

static class ProtocolState
{
    internal enum Name
    {
        Syncing,
        Synchronized,
        Running,
        Disconnected,
    }

    internal class Sync
    {
        public uint RemainingRoundtrips;
        public uint Random;
    }

    internal class Running
    {
        public uint LastQualityReportTime;
        public uint LastNetworkStatsInterval;
        public uint LastInputPacketRecvTime;
    }

    internal sealed class Udp
    {
        public readonly Sync Sync = new();
        public readonly Running Running = new();
    }
}

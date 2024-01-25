namespace nGGPO.Network.Protocol;

sealed class ProtocolState
{
    internal class SyncState
    {
        public uint RemainingRoundtrips;
        public uint Random;
    }

    internal class RunningState
    {
        public uint LastQualityReportTime;
        public uint LastNetworkStatsInterval;
        public uint LastInputPacketRecvTime;
    }

    public readonly SyncState Sync = new();
    public readonly RunningState Running = new();
}

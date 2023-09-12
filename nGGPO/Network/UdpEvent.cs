namespace nGGPO.Network;

class UdpEvent
{
    public enum TypeEnum
    {
        Unknown = -1,
        Connected,
        Synchronizing,
        Synchronzied,
        Input,
        Disconnected,
        NetworkInterrupted,
        NetworkResumed,
    };

    public class SynchronizingData
    {
        public int Total { get; set; }
        public int Count { get; set; }
    }

    public class NetworkInterruptedData
    {
        public int DisconnectTimeout { get; set; }
    }

    public TypeEnum Type { get; set; } = TypeEnum.Unknown;
    public GameInput? Input { get; set; }
    public SynchronizingData? Synchronizing { get; set; }
    public NetworkInterruptedData? NetworkInterrupted { get; set; }
}
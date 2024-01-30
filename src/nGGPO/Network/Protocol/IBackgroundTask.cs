namespace nGGPO.Network.Protocol;

public interface IBackgroundTask
{
    Task Start(CancellationToken ct);
}

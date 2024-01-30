namespace nGGPO.Utils;

public interface IBackgroundTask
{
    Task Start(CancellationToken ct);
}

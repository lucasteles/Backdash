namespace nGGPO.Lifecycle;

public interface IBackgroundJob
{
    Task Start(CancellationToken ct);
}

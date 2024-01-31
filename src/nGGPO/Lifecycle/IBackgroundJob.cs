namespace nGGPO.Lifecycle;

public interface IBackgroundJob
{
    string JobName { get; }
    Task Start(CancellationToken ct);
}

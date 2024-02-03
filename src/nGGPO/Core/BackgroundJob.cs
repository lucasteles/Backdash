namespace nGGPO.Core;

public interface IBackgroundJob
{
    string JobName { get; }
    Task Start(CancellationToken ct);
}

interface IBackgroundJobManager : IAsyncDisposable
{
    Task Start(CancellationToken ct);
    void Register(IBackgroundJob job);
}

sealed class BackgroundJobManager(ILogger logger) : IBackgroundJobManager
{
    readonly HashSet<IBackgroundJob> jobs = [];
    readonly Dictionary<Task, IBackgroundJob> tasks = [];
    readonly CancellationTokenSource cts = new();

    CancellationTokenSource? linkedCts;
    bool started;

    CancellationToken StoppingToken => linkedCts?.Token ?? cts.Token;

    public async Task Start(CancellationToken ct)
    {
        if (started) return;
        if (jobs.Count is 0) throw new NggpoException("No jobs registered");
        linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);

        logger.Info($"Starting background tasks");
        foreach (var job in jobs)
        {
            var task = Task.Run(() => job.Start(StoppingToken), StoppingToken);
            tasks.Add(task, job);
        }

        started = true;

        while (tasks.Keys.Any(x => !x.IsCompleted))
        {
            var completed = await Task.WhenAny(tasks.Keys).ConfigureAwait(false);

            if (!tasks.TryGetValue(completed, out var completedJob))
                continue;

            logger.Trace($"Completed: {completedJob.JobName}");
            jobs.Remove(completedJob);
            tasks.Remove(completed);
        }

        logger.Info($"Finished background tasks");
    }

    public void Register(IBackgroundJob job)
    {
        jobs.Add(job);

        if (started)
            tasks.Add(Task.Run(() => job.Start(StoppingToken), StoppingToken), job);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await cts.CancelAsync().ConfigureAwait(false);
        }
        finally
        {
            cts.Dispose();
        }

        linkedCts?.Dispose();
    }
}

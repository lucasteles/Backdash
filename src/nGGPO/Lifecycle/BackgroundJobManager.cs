namespace nGGPO.Lifecycle;

interface IBackgroundJobManager : IAsyncDisposable
{
    Task Start(CancellationToken ct);
    void Register(IBackgroundJob job);
}

sealed class BackgroundJobManager : IBackgroundJobManager
{
    readonly List<IBackgroundJob> jobs = [];
    readonly Dictionary<Task, IBackgroundJob> tasks = [];
    readonly CancellationTokenSource cts = new();

    CancellationTokenSource? linkedCts;
    bool started;

    CancellationToken StoppingToken => linkedCts?.Token ?? cts.Token;

    public async Task Start(CancellationToken ct)
    {
        if (started) return;
        if (jobs is []) throw new NggpoException("No jobs registered");
        linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);

        foreach (var job in jobs)
        {
            var task = job.Start(StoppingToken);
            tasks.Add(task, job);
        }

        started = true;

        while (tasks.Keys.Any(x => !x.IsCompleted))
        {
            var completed = await Task.WhenAny(tasks.Keys).ConfigureAwait(false);

            if (!tasks.TryGetValue(completed, out var completedJob))
                continue;

            jobs.Remove(completedJob);
            tasks.Remove(completed);
        }
    }

    public void Register(IBackgroundJob job)
    {
        jobs.Add(job);

        if (started)
            tasks.Add(job.Start(StoppingToken), job);
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

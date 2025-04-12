using System.Threading.Channels;

namespace Backdash.Core;

interface IBackgroundJob
{
    string JobName { get; }

    Task Start(CancellationToken cancellationToken);
}

interface IBackgroundJobManager : IDisposable
{
    Task Start(bool onThread, CancellationToken cancellationToken);
    void Register(IBackgroundJob job, CancellationToken cancellationToken = default);
    void Stop(TimeSpan timeout = default);
    void ThrowIfError();
    bool IsRunning { get; }
}

sealed class BackgroundJobManager(Logger logger) : IBackgroundJobManager
{
    readonly HashSet<JobEntry> jobs = [];
    readonly Dictionary<Task, JobEntry> tasks = [];
    readonly CancellationTokenSource cts = new();
    CancellationToken StoppingToken => cts.Token;

    readonly List<Exception> exceptions = [];

    public bool IsRunning { get; private set; }

    public Task Start(bool onThread, CancellationToken cancellationToken) =>
        onThread
            ? Task.Run(() => StartJobs(cancellationToken), StoppingToken)
            : StartJobs(cancellationToken);

    public async Task StartJobs(CancellationToken cancellationToken)
    {
        if (IsRunning) return;
        if (jobs.Count is 0) throw new NetcodeException("No jobs registered");

        logger.Write(LogLevel.Debug, "Starting background tasks");
        cancellationToken.Register(() => Stop(TimeSpan.Zero));

        foreach (var job in jobs)
            AddJobTask(new(job.Job, job.StoppingToken));

        IsRunning = true;

        while (tasks.Keys.Any(x => !x.IsCompleted))
        {
            var completed = await Task.WhenAny(tasks.Keys).ConfigureAwait(false);
            if (!tasks.TryGetValue(completed, out var completedJob))
                continue;
            logger.Write(LogLevel.Debug, $"Completed: {completedJob.Name}");
            jobs.Remove(completedJob);
            tasks.Remove(completed);
        }

        logger.Write(LogLevel.Debug, "Finished background tasks");
    }

    void AddJobTask(JobEntry entry) => tasks.Add(StarJobTask(entry), entry);

    async Task StarJobTask(JobEntry entry)
    {
        var job = entry.Job;
        logger.Write(LogLevel.Trace, $"job {job.JobName}: start");
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(StoppingToken, entry.StoppingToken);
        try
        {
            await job.Start(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.Write(LogLevel.Debug, $"job {job.JobName} stopped");
        }
        catch (ChannelClosedException)
        {
            logger.Write(LogLevel.Debug, $"job {job.JobName} channel closed");
        }
        catch (NetcodeAssertionException ex) when (StoppingToken.IsCancellationRequested)
        {
            logger.Write(LogLevel.Debug, $"job {job.JobName} skip assert: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.Write(LogLevel.Error, $"job {job.JobName} error: {ex}");
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));
            exceptions.Add(ex);
        }
    }

    public void ThrowIfError()
    {
        if (exceptions.Count is 0) return;
        throw new AggregateException(exceptions);
    }

    public void Register(IBackgroundJob job, CancellationToken cancellationToken = default)
    {
        JobEntry entry = new(job, cancellationToken);
        if (!jobs.Add(entry)) return;
        if (IsRunning)
            AddJobTask(entry);
    }

    public void Stop(TimeSpan timeout = default)
    {
        if (!IsRunning) return;
        IsRunning = false;
        if (cts.IsCancellationRequested) return;

        if (timeout <= TimeSpan.Zero)
            cts.Cancel();
        else
            cts.CancelAfter(timeout);
    }

    public void Dispose()
    {
        try
        {
            Stop(TimeSpan.Zero);
        }
        finally
        {
            cts.Dispose();
        }
    }

    readonly struct JobEntry(IBackgroundJob job, CancellationToken stoppingToken) : IEquatable<JobEntry>
    {
        public readonly IBackgroundJob Job = job;
        public readonly CancellationToken StoppingToken = stoppingToken;
        public string Name => Job.JobName;
        public bool Equals(JobEntry other) => Name.Equals(other.Name);
        public override bool Equals(object? obj) => obj is JobEntry other && Equals(other);
        public override int GetHashCode() => Name.GetHashCode();
        public static bool operator ==(JobEntry left, JobEntry right) => left.Equals(right);
        public static bool operator !=(JobEntry left, JobEntry right) => !left.Equals(right);
    }
}

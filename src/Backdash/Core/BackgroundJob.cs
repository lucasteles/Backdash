using System.Threading.Channels;

namespace Backdash.Core;

interface IBackgroundJob
{
    string JobName { get; }

    Task Start(CancellationToken ct);
}

interface IBackgroundJobManager : IDisposable
{
    Task Start(CancellationToken ct);
    void Register(IBackgroundJob job, CancellationToken ct = default);
    void Stop(TimeSpan timeout = default);
    void ThrowIfError();
}

sealed class BackgroundJobManager(Logger logger) : IBackgroundJobManager
{
    readonly HashSet<JobEntry> jobs = [];
    readonly Dictionary<Task, JobEntry> tasks = [];
    readonly CancellationTokenSource cts = new();
    bool isRunning;
    CancellationToken StoppingToken => cts.Token;

    readonly List<Exception> exceptions = [];

    public async Task Start(CancellationToken ct)
    {
        if (isRunning) return;
        if (jobs.Count is 0) throw new NetcodeException("No jobs registered");
        ct.Register(() => Stop(TimeSpan.Zero));
        logger.Write(LogLevel.Debug, "Starting background tasks");
        foreach (var job in jobs) AddJobTask(new(job.Job, job.StoppingToken));
        isRunning = true;
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

    void AddJobTask(JobEntry entry)
    {
        var job = entry.Job;
        logger.Write(LogLevel.Trace, $"job {job.JobName} start");
        var task = Task.Run(async () =>
        {
            var jobCancellation = entry.StoppingToken;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(StoppingToken, jobCancellation);
            try
            {
                await job.Start(linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                logger.Write(LogLevel.Trace, $"job {job.JobName} stopped");
            }
            catch (ChannelClosedException)
            {
                logger.Write(LogLevel.Trace, $"job {job.JobName} channel closed");
            }
            catch (Exception ex)
            {
                logger.Write(LogLevel.Error, $"job {job.JobName} error: {ex}");
                cts.CancelAfter(TimeSpan.FromMilliseconds(100));
                exceptions.Add(ex);
            }
        }, StoppingToken);
        tasks.Add(task, entry);
    }

    public void ThrowIfError()
    {
        if (exceptions.Count is 0) return;
        throw new AggregateException(exceptions);
    }

    public void Register(IBackgroundJob job, CancellationToken ct = default)
    {
        JobEntry entry = new(job, ct);
        if (!jobs.Add(entry))
            return;
        if (!isRunning) return;
        AddJobTask(entry);
    }

    public void Stop(TimeSpan timeout = default)
    {
        if (!isRunning) return;
        isRunning = false;
        if (!cts.IsCancellationRequested)
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

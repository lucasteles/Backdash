using System.Threading.Channels;

namespace Backdash.Core;

/// <summary>
/// Defines an asynchronous background job
/// </summary>
public interface INetcodeJob
{
    /// <summary>
    /// Job name identity
    /// </summary>
    string? JobName { get; }

    /// <summary>
    /// Job task
    /// </summary>
    Task Start(CancellationToken cancellationToken);
}

sealed class NetcodeJobManager(Logger logger) : IDisposable
{
    readonly HashSet<JobEntry> jobs = [];
    readonly Dictionary<Task, JobEntry> tasks = [];
    readonly CancellationTokenSource cts = new();
    readonly object locker = new();

    CancellationToken StoppingToken => cts.Token;

    readonly List<Exception> exceptions = [];

    public bool IsRunning { get; private set; }

    public Task Start(bool onThread, CancellationToken cancellationToken) =>
        onThread
            ? Task.Run(() => StartJobs(cancellationToken), StoppingToken)
            : StartJobs(cancellationToken);

    async Task StartJobs(CancellationToken cancellationToken)
    {
        lock (locker)
            if (IsRunning)
                return;

        logger.Write(LogLevel.Debug, "Starting background tasks");
        cancellationToken.Register(() => Stop(TimeSpan.Zero));

        lock (locker)
        {
            IsRunning = true;
            foreach (var job in jobs)
                AddJobTask(new(job.Job, job.StoppingToken));
        }

        await WaitJobsToFinish();
        logger.Write(LogLevel.Debug, "Finished background tasks");
    }

    async Task WaitJobsToFinish()
    {
        while (tasks.Keys.Any(x => !x.IsCompleted))
        {
            var completed = await Task.WhenAny(tasks.Keys).ConfigureAwait(false);
            if (!tasks.TryGetValue(completed, out var completedJob)) continue;

            logger.Write(LogLevel.Debug, $"Completed: {completedJob.Name}");
            jobs.Remove(completedJob);
            tasks.Remove(completed);
        }
    }

    void AddJobTask(JobEntry entry)
    {
        var task = StarJobTask(entry);

        if (task.IsCompleted)
        {
            jobs.Remove(entry);
            return;
        }

        tasks.Add(task, entry);
    }

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

    public void Register(INetcodeJob job, CancellationToken cancellationToken = default)
    {
        lock (locker)
        {
            JobEntry entry = new(job, cancellationToken);
            if (!jobs.Add(entry)) return;
            if (IsRunning)
                AddJobTask(entry);
        }
    }

    public void Stop(TimeSpan timeout = default)
    {
        lock (locker)
        {
            if (!IsRunning) return;
            IsRunning = false;
        }

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

    readonly struct JobEntry(INetcodeJob job, CancellationToken stoppingToken) : IEquatable<JobEntry>
    {
        public readonly INetcodeJob Job = job;
        public readonly CancellationToken StoppingToken = stoppingToken;
        public string Name { get; } = job.JobName ?? job.GetType().Name;
        public bool Equals(JobEntry other) => Name.Equals(other.Name);
        public override bool Equals(object? obj) => obj is JobEntry other && Equals(other);
        public override int GetHashCode() => Name.GetHashCode();
        public static bool operator ==(JobEntry left, JobEntry right) => left.Equals(right);
        public static bool operator !=(JobEntry left, JobEntry right) => !left.Equals(right);
    }
}

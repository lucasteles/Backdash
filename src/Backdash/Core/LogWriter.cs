namespace Backdash.Core;

public interface ILogWriter : IDisposable
{
    void Write(LogLevel level, char[] chars, int size);
}

public abstract class LogWriter : ILogWriter
{
    protected abstract TextWriter textWriter { get; }
    readonly object locker = new();
    bool disposed;

    public void Write(LogLevel level, char[] chars, int size)
    {
        if (disposed) return;

        lock (locker)
            textWriter.WriteLine(chars, 0, size);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        disposed = true;
        lock (locker)
        {
            textWriter.Close();
            textWriter.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

public sealed class ConsoleLogWriter : LogWriter
{
    protected override TextWriter textWriter { get; } = Console.Out;
}

public sealed class FileLogWriter : LogWriter
{
    protected override TextWriter textWriter { get; }

    const string DefaultFileName = "log_{{proc_id}}_{{timestamp}}";

    public FileLogWriter(string? filename = null, bool append = true)
    {
        filename = !string.IsNullOrWhiteSpace(filename) ? filename : DefaultFileName;
        filename = filename
            .Replace("{{proc_id}}", Environment.ProcessId.ToString())
            .Replace("{{timestamp}}", $"{DateTime.Now:yyyyMMddhhmmss}");

        textWriter = new StreamWriter(filename, append)
        {
            AutoFlush = true,
        };
    }
}

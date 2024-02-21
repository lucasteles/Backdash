namespace Backdash.Core;

public interface ILogWriter : IDisposable
{
    void Write(LogLevel level, char[] chars, int size);
}

public abstract class LogWriter : ILogWriter, IAsyncDisposable
{
    protected abstract TextWriter textWriter { get; }
    readonly object locker = new();

    public void Write(LogLevel level, char[] chars, int size)
    {
        lock (locker)
            textWriter.WriteLine(chars, 0, size);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
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

    public async ValueTask DisposeAsync()
    {
        await textWriter.DisposeAsync();
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

    public FileLogWriter(string? filename = null)
    {
        filename = !string.IsNullOrWhiteSpace(filename) ? filename : DefaultFileName;
        filename = filename
            .Replace("{{proc_id}}", Environment.ProcessId.ToString())
            .Replace("{{timestamp}}", $"{DateTime.Now:yyyyMMddhhmmss}");

        textWriter = new StreamWriter(filename, append: true);
    }
}

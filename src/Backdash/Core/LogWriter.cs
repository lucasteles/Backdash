namespace Backdash.Core;

/// <summary>
/// Defines how and where the log's should be written.
/// </summary>
public interface ILogWriter : IDisposable
{
    /// <summary>
    /// Write <paramref name="chars"/> into an output.
    /// </summary>
    /// <param name="level">Current <see cref="LogLevel"/> level.</param>
    /// <param name="chars">Char buffer array containing the log message</param>
    /// <param name="size">Number of chars of characters to read from <paramref name="chars"/></param>
    void Write(LogLevel level, char[] chars, int size);
}

/// <summary>
/// Base implementation of <see cref="ILogWriter"/> for any <see cref="textWriter"/>.
/// </summary>
public abstract class TextLogWriter : ILogWriter
{
    /// <summary>
    /// Current <see cref="TextWriter"/>
    /// </summary>
    protected abstract TextWriter textWriter { get; }

    readonly object locker = new();
    bool disposed;

    /// <inheritdoc />
    public void Write(LogLevel level, char[] chars, int size)
    {
        if (disposed) return;
        lock (locker)
            textWriter.WriteLine(chars, 0, size);
    }

    /// <summary>
    /// Releases all resources currently used by this <see cref="TextLogWriter"/> instance.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> if this method is being invoked by the <see cref="Dispose()"/> method,
    /// otherwise <see langword="false"/>.</param>
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

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Implementation of <see cref="ILogWriter"/> for logging into <see cref="Console"/>.
/// </summary>
public sealed class ConsoleTextLogWriter : TextLogWriter
{
    /// <inheritdoc />
    protected override TextWriter textWriter { get; } = Console.Out;
}

/// <summary>
/// Implementation of <see cref="ILogWriter"/> for logging into a file.
/// </summary>
public sealed class FileTextLogWriter : TextLogWriter
{
    /// <inheritdoc />
    protected override TextWriter textWriter { get; }

    const string DefaultFileName = "{{proc_id}}_{{timestamp}}.log";

    /// <summary>
    /// Initializes a new instance of the <see cref="FileTextLogWriter"/> class.
    /// </summary>
    /// <param name="filename">Log file name<remarks>
    /// you can use placeholders for:
    /// - <see cref="Environment.ProcessId"/>: <c>"{{proc_id}}"</c>;
    /// - <see cref="DateTime.UtcNow"/>: <c>"{{timestamp}}"</c>;
    /// </remarks>
    /// <value>Defaults to <c>"{{proc_id}}_{{timestamp}}.log"</c></value>
    /// </param>
    /// <param name="append">
    /// true to append data to the file; false to overwrite the file. If the specified file does not exist, this parameter has no effect, and the constructor creates a new file.
    /// </param>
    public FileTextLogWriter(string? filename = null, bool append = true)
    {
        filename = !string.IsNullOrWhiteSpace(filename) ? filename : DefaultFileName;
        filename = filename
            .Replace("{{proc_id}}", Environment.ProcessId.ToString())
            .Replace("{{timestamp}}", $"{DateTime.UtcNow:yyyyMMddhhmmss}");
        textWriter = new StreamWriter(filename, append)
        {
            AutoFlush = true,
        };
    }
}

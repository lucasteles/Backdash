using System.Runtime.CompilerServices;
using System.Text;

namespace nGGPO.Core;

public enum LogLevel : byte
{
    Off,
    Trace,
    Information,
    Warning,
    Error,
}

[InterpolatedStringHandler]
public readonly ref struct LogStringHandler
{
    readonly StringBuilder? builder;

    public LogStringHandler(int literalLength, int formattedCount)
    {
        builder = new(literalLength);

        builder.Append('[');
        builder.Append(Environment.CurrentManagedThreadId);
        builder.Append(']');
        builder.Append(' ');
    }

    public void AppendLiteral(string s) => builder?.Append(s);

    public void AppendFormatted<T>(T t) => builder?.Append(t);

    internal string GetFormattedText() => builder is null ? string.Empty : builder.ToString();
}

public interface ILogger
{
    LogLevel EnabledLevel { get; init; }
    void Message(LogLevel level, LogStringHandler builder);

    internal void Trace(LogStringHandler builder) => Message(LogLevel.Trace, builder);
    internal void Info(LogStringHandler builder) => Message(LogLevel.Information, builder);
    internal void Warn(LogStringHandler builder) => Message(LogLevel.Warning, builder);
    internal void Error(LogStringHandler builder) => Message(LogLevel.Error, builder);

    internal void Error(Exception e, LogStringHandler builder)
    {
        builder.AppendLiteral(e.Message);
        Error(builder);
    }
}

sealed class ConsoleLogger : ILogger
{
    public required LogLevel EnabledLevel { get; init; }

    public void Message(LogLevel level, LogStringHandler builder)
    {
        if (EnabledLevel is LogLevel.Off || level < EnabledLevel) return;
        Console.WriteLine(builder.GetFormattedText());
    }
}

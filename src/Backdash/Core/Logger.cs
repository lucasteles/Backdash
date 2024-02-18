using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Backdash.Core;

public enum LogLevel : byte
{
    Off,
    Trace,
    Debug,
    Information,
    Warning,
    Error,
}

public interface ILogWriter
{
    void Write(LogLevel level, string text);
    void Write(LogLevel level, char[] chars, int size);
}

sealed class Logger(LogLevel level, ILogWriter writer)
{
    public readonly LogLevel EnabledLevel = level;

    readonly ArrayPool<char> pool = ArrayPool<char>.Shared;


    public void Write(
        LogLevel level,
        [InterpolatedStringHandlerArgument("", "level")]
        LogInterpolatedStringHandler builder
    )
    {
        if (!builder.Enabled || !IsEnabledFor(in level)) return;
        Span<byte> utf8Bytes = builder.Buffer[..builder.Length];
        var charCount = Encoding.UTF8.GetCharCount(utf8Bytes);
        var buffer = pool.Rent(charCount);
        try
        {
            Encoding.UTF8.GetChars(utf8Bytes, buffer);
            writer.Write(level, buffer, builder.Length);
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    public void Write(LogLevel level, string text)
    {
        if (level < EnabledLevel) return;
        writer.Write(level, text);
    }

    public bool IsEnabledFor(in LogLevel level) => level >= EnabledLevel;

    internal static Logger CreateConsoleLogger(LogLevel level) => new(level, new ConsoleLogWriter());
}

sealed class ConsoleLogWriter : ILogWriter
{
    readonly object locker = new();

    public void Write(LogLevel level, string text)
    {
        lock (locker)
        {
            WriteLevel(in level);
            Console.Out.WriteLine(text);
        }
    }

    public void Write(LogLevel level, char[] chars, int size)
    {
        lock (locker)
        {
            WriteLevel(in level);
            Console.Out.WriteLine(chars, 0, size);
        }
    }

    void WriteLevel(in LogLevel level) =>
        Console.Out.Write(level switch
        {
            LogLevel.Trace => "trc: ",
            LogLevel.Debug => "dbg: ",
            LogLevel.Information => "inf: ",
            LogLevel.Warning => "warn: ",
            LogLevel.Error => "err: ",
            LogLevel.Off => throw new InvalidOperationException(),
            _ => "",
        });
}

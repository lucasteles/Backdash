using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace Backdash.Core;

using DeferredLog = (LogLevel Level, LogStringBuffer Buffer, int Length);

public enum LogLevel : byte
{
    Off,
    Trace,
    Debug,
    Information,
    Warning,
    Error,
}

sealed class Logger : IDisposable, IBackgroundJob
{
    public readonly LogLevel EnabledLevel;
    public readonly bool RunningAsync;

    readonly long start = Stopwatch.GetTimestamp();
    readonly ArrayPool<char> pool = ArrayPool<char>.Shared;

    readonly Channel<DeferredLog>? queue;
    readonly LogOptions options;
    readonly ILogWriter writer;

    public Logger(LogOptions options,
        ILogWriter writer)
    {
        this.options = options;
        this.writer = writer;
        EnabledLevel = options.EnabledLevel;
        JobName = $"Log Flush {writer.GetType()}";
        RunningAsync = options.RunAsync;

        if (RunningAsync)
            queue = Channel.CreateUnbounded<DeferredLog>(new()
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = true,
            });
    }

    public void Write(LogLevel level, string text) => Write(level, $"{text}");

    public void Write(
        LogLevel level,
        [InterpolatedStringHandlerArgument("", "level")]
        LogInterpolatedStringHandler builder
    )
    {
        if (!builder.Enabled || !IsEnabledFor(in level)) return;
        if (RunningAsync)
            WriteLater(in level, in builder);
        else
            WriteNow(in level, in builder.Buffer, in builder.Length);
    }

    void WriteLater(in LogLevel level, in LogInterpolatedStringHandler builder) =>
        queue?.Writer.TryWrite((level, builder.Buffer, builder.Length));

    void WriteNow(in LogLevel level, in LogStringBuffer logBuffer, in int length)
    {
        ReadOnlySpan<byte> utf8Bytes = logBuffer[..length];
        var charCount = Encoding.UTF8.GetCharCount(utf8Bytes);
        var buffer = pool.Rent(charCount);
        try
        {
            Encoding.UTF8.GetChars(utf8Bytes, buffer);
            writer.Write(level, buffer, length);
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    public bool IsEnabledFor(in LogLevel level) => level >= EnabledLevel;

    public static Logger CreateConsoleLogger(LogLevel level) => new(
        new LogOptions
        {
            EnabledLevel = level,
        },
        new ConsoleLogWriter()
    );

    public void AppendTimestamp(ref LogInterpolatedStringHandler builder)
    {
        if (!options.AppendTimestamps) return;
        var elapsed = Stopwatch.GetElapsedTime(start);
        builder.AppendFormatted(elapsed, options.TimestampFormat);
        builder.AppendFormatted(" "u8);
    }

    public void AppendLevel(in LogLevel level, ref LogInterpolatedStringHandler builder)
    {
        if (!options.AppendLevel) return;
        builder.AppendFormatted(ShortLevelName(level));
        builder.AppendFormatted(": "u8);
    }

    public void AppendThreadId(ref LogInterpolatedStringHandler builder)
    {
        if (!options.AppendThreadId) return;
        builder.AppendFormatted("["u8);
        builder.AppendFormatted(Environment.CurrentManagedThreadId);
        builder.AppendFormatted("] "u8);
    }

    static string ShortLevelName(LogLevel level) =>
        level switch
        {
            LogLevel.Trace => "trc",
            LogLevel.Debug => "dbg",
            LogLevel.Information => "inf",
            LogLevel.Warning => "warn",
            LogLevel.Error => "err",
            LogLevel.Off => throw new InvalidOperationException(),
            _ => "",
        };

    public void Dispose()
    {
        writer.Dispose();
        queue?.Writer.TryComplete();
    }

    public string JobName { get; }

    public async Task Start(CancellationToken ct)
    {
        if (!RunningAsync || queue is null)
            return;

        while (!ct.IsCancellationRequested)
        {
            await queue.Reader.WaitToReadAsync(ct).ConfigureAwait(false);

            while (queue.Reader.TryRead(out var entry))
                WriteNow(entry.Level, entry.Buffer, entry.Length);
        }
    }
}

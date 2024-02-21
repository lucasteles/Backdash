namespace Backdash.Core;

public sealed class LogOptions
{
    public LogLevel EnabledLevel { get; init; } =
#if DEBUG
        LogLevel.Debug;
#else
        LogLevel.Warning;
#endif

    public bool AppendTimestamps { get; init; } = true;
    public bool AppendLevel { get; init; } = true;
    public string TimestampFormat { get; init; } = @"mm\:ss\.fff";
    public bool RunAsync { get; init; }
    public bool AppendThreadId { get; init; }
}

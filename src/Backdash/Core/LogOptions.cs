namespace Backdash.Core;

public sealed class LogOptions(
    LogLevel level =
#if DEBUG
        LogLevel.Information
#else
        LogLevel.Warning
#endif
)
{
    public LogLevel EnabledLevel { get; init; } = level;
    public bool AppendTimestamps { get; init; } = true;
    public bool AppendLevel { get; init; } = true;
    public string TimestampFormat { get; init; } = @"mm\:ss\.fff";
    public bool RunAsync { get; init; }
    public bool AppendThreadId { get; init; }
}

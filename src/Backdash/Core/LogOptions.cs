namespace Backdash.Core;

/// <summary>
/// Specifies options common to logging
/// </summary>
/// <param name="level"><see cref="EnabledLevel"/> value.</param>
public sealed class LogOptions(
    LogLevel level =
#if DEBUG
        LogLevel.Information
#else
        LogLevel.Warning
#endif
)
{
    /// <summary>
    /// Gets or sets the enabled <see cref="LogLevel"/>
    /// </summary>
    public LogLevel EnabledLevel { get; init; } = level;

    /// <summary>
    /// Gets or sets a value indicating whether timestamps should be prepended to logs.
    /// <value>Defaults to <see langword="true"/></value>
    /// </summary>
    public bool AppendTimestamps { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether level name should be prepended to logs.
    /// <value>Defaults to <see langword="true"/></value>
    /// </summary>
    public bool AppendLevel { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the format for log timestamps.
    /// <value>Defaults to "mm:ss.fff"</value>
    /// </summary>
    public string TimestampFormat { get; init; } = @"mm\:ss\.fff";

    /// <summary>
    /// Gets or sets a value indicating whether thread id should be prepended to logs.
    /// <value>Defaults to <see langword="false"/></value>
    /// </summary>
    public bool AppendThreadId { get; init; }
}

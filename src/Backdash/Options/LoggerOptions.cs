using Backdash.Core;

namespace Backdash.Options;

/// <summary>
///     Specifies options common to logging.
/// </summary>
public sealed record LoggerOptions
{
    /// <summary>
    ///     Specifies options common to logging.
    /// </summary>
    /// <param name="level"><see cref="EnabledLevel" /> value</param>
    public LoggerOptions(
        LogLevel level =
#if DEBUG
            LogLevel.Information
#else
        LogLevel.Warning
#endif
    ) => EnabledLevel = level;

    /// <summary>
    ///     Gets or sets the enabled <see cref="LogLevel" />.
    /// </summary>
    public LogLevel EnabledLevel { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether timestamps should be prepended to logs.
    ///     <value>Defaults to <see langword="true" /></value>
    /// </summary>
    public bool AppendTimestamps { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether level name should be prepended to logs.
    ///     <value>Defaults to <see langword="true" /></value>
    /// </summary>
    public bool AppendLevel { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether the format for log timestamps.
    ///     <value>Defaults to "mm:ss.fff"</value>
    /// </summary>
    public string TimestampFormat { get; set; } = @"mm\:ss\.fff";

    /// <summary>
    ///     Gets or sets a value indicating whether thread id should be prepended to logs.
    ///     <value>Defaults to <see langword="false" /></value>
    /// </summary>
    public bool AppendThreadId { get; set; }

    /// <summary>
    ///     Output text logs only
    /// </summary>
    public LoggerOptions RawLogs()
    {
        AppendLevel = false;
        AppendTimestamps = false;
        AppendThreadId = false;
        return this;
    }
}

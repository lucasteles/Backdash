using System.Net;
using System.Runtime.CompilerServices;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Comm;
using Backdash.Serialization.Buffer;

#pragma warning disable S4144
namespace Backdash.Core;

[InterpolatedStringHandler]
ref struct LogInterpolatedStringHandler
{
    public LogStringBuffer Buffer;
    public int Length;
    public readonly bool Enabled;

    public LogInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, LogLevel level,
        out bool isEnabled)
    {
        isEnabled = logger.IsEnabledFor(level);
        Enabled = isEnabled;
        if (!isEnabled) return;
        Buffer = new();
        Length = 0;

        logger.AppendTimestamp(ref this);
        logger.AppendLevel(level, ref this);
        logger.AppendThreadId(ref this);
    }

    public void AppendLiteral(ReadOnlySpan<char> t)
    {
        if (!Enabled) return;
        Utf8StringWriter writer = new(Buffer, ref Length);
        writer.WriteChars(t);
    }

    public void AppendFormatted(ReadOnlySpan<char> t)
    {
        if (!Enabled) return;
        Utf8StringWriter writer = new(Buffer, ref Length);
        writer.WriteChars(t);
    }

    public void AppendFormatted(ReadOnlySpan<byte> t)
    {
        if (!Enabled) return;
        Utf8StringWriter writer = new(Buffer, ref Length);
        writer.Write(t);
    }

    public void AppendFormatted(bool t)
    {
        if (!Enabled) return;
        Utf8StringWriter writer = new(Buffer, ref Length);
        writer.Write(t ? "true"u8 : "false"u8);
    }

    public void AppendFormatted(Exception t)
    {
        if (!Enabled) return;
        Utf8StringWriter writer = new(Buffer, ref Length);
        writer.WriteChars(t.Message);
    }

    public void AppendFormatted(IPAddress t)
    {
        if (!Enabled) return;
        Utf8StringWriter writer = new(Buffer, ref Length);
        writer.WriteFormat(t);
    }

    public void AppendFormatted<T>(T t) where T : struct, IUtf8SpanFormattable
    {
        if (!Enabled) return;
        Utf8StringWriter writer = new(Buffer, ref Length);
        writer.Write(t);
    }

    public void AppendFormatted<T>(T t, ReadOnlySpan<char> format) where T : IUtf8SpanFormattable
    {
        if (!Enabled) return;
        Utf8StringWriter writer = new(Buffer, ref Length);
        writer.Write(t, format);
    }

    public void AppendFormatted(ProtocolStatus status)
    {
        if (!Enabled) return;
        Utf8StringWriter writer = new(Buffer, ref Length);
        writer.WriteEnum(status);
    }

    public void AppendFormatted(SendInputResult result)
    {
        if (!Enabled) return;
        Utf8StringWriter writer = new(Buffer, ref Length);
        writer.WriteEnum(result);
    }
}

[InlineArray(Capacity)]
struct LogStringBuffer
{
    public const int Capacity = 128;

    byte elemenet0;
}

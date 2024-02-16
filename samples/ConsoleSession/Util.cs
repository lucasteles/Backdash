using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Backdash;
using Backdash.Core;

namespace ConsoleSession;

static class Util
{
    public static bool TryParsePlayer(int number, string address,
        [NotNullWhen(true)] out Player? player)
    {
        if (address.Equals("local", StringComparison.OrdinalIgnoreCase))
        {
            player = new Player.Local(number);
            return true;
        }

        if (IPEndPoint.TryParse(address, out var endPoint))
        {
            player = new Player.Remote(number, endPoint);
            return true;
        }

        player = null;
        return false;
    }
}

public sealed class DebuggerLogger : ILogWriter
{
    public LogLevel EnabledLevel { get; set; }

    public void Write(LogLevel level, char[] chars, int size) =>
        Write(level, new string(chars.AsSpan()[..size]));

    public void Write(LogLevel level, string text)
    {
        Trace.Write(level switch
        {
            LogLevel.Trace => "trc: ",
            LogLevel.Debug => "dbg: ",
            LogLevel.Information => "inf: ",
            LogLevel.Warning => "warn: ",
            LogLevel.Error => "err: ",
            LogLevel.Off => throw new InvalidOperationException(),
            _ => "",
        });
        Trace.WriteLine(text);
    }
}
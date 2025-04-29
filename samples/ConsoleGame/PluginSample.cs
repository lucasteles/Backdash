using System.Net;
using Backdash;
using Backdash.Core;
using Backdash.Options;

namespace ConsoleGame;

// Sample in how to add a hook plugin into backdash
public sealed class PluginSample : INetcodePlugin
{
    TextWriter? textWriter;

    void Log(string message) => textWriter?.WriteLine($"{DateTime.UtcNow:s} PLUGIN => {message}");

    public void OnSessionStart(INetcodeSession session)
    {
        var suffix = "";
        if (session.TryGetLocalPlayer(out var player))
            suffix = $"player_{player.Number}";

        var fileName = $"logs/log_plugin_{session.Mode}_{suffix}.log";
        textWriter = FileTextLogWriter.GetLogFileWriter(fileName, false);

        Log("Starting Session");
    }

    public void OnSessionClose(INetcodeSession session) => Log("Closing Session");

    public void OnEndpointAdded(INetcodeSession session, EndPoint endpoint, NetcodePlayer player) =>
        Log($"Added Endpoint: {endpoint} for {player}");

    public void OnEndpointClosed(INetcodeSession session, EndPoint endpoint, NetcodePlayer player) =>
        Log($"Closing Endpoint: {endpoint} for {player}");

    public void Dispose()
    {
        Log("Disposing");
        textWriter?.Dispose();
    }
}

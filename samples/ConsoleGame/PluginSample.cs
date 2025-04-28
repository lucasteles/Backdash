using System.Net;
using Backdash;
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

        var fileName = $"plugin_log_{session.Mode}_{suffix}.txt";
        textWriter = new StreamWriter(fileName.ToLowerInvariant(), false)
        {
            AutoFlush = true,
        };

        Log("Starting Session");
    }

    public void OnSessionClose(INetcodeSession session) => Log("Closing Session");

    public void OnEndpointAdded(INetcodeSession session, EndPoint endpoint, in PlayerHandle player) =>
        Log($"Added Endpoint: {endpoint} for {player}");

    public void OnEndpointClosed(INetcodeSession session, EndPoint endpoint, in PlayerHandle player) =>
        Log($"Closing Endpoint: {endpoint} for {player}");

    public void Dispose()
    {
        Log("Disposing");
        textWriter?.Dispose();
    }
}

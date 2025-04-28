using System.Collections.Immutable;
using Backdash.Network;
using Backdash.Options;

namespace Backdash.Core;

sealed class PluginManager(
    Logger logger,
    IEnumerable<INetcodePlugin> plugins
) : IDisposable
{
    readonly ImmutableArray<INetcodePlugin> plugins = [.. plugins];

    public void OnStart(INetcodeSession session)
    {
        foreach (var plugin in plugins)
            try
            {
                plugin?.OnSessionStart(session);
            }
            catch (Exception e)
            {
                logger.Write(LogLevel.Error, $"plugin start error: {e}");
            }
    }

    public void OnClose(INetcodeSession session)
    {
        foreach (var plugin in plugins)
            try
            {
                plugin?.OnSessionClose(session);
            }
            catch (Exception e)
            {
                logger.Write(LogLevel.Error, $"plugin close error: {e}");
            }
    }

    public void OnEndpointClosed(INetcodeSession session, PeerAddress address, NetcodePlayer player)
    {
        foreach (var plugin in plugins)
            plugin.OnEndpointClosed(session, address.EndPoint, player);
    }

    public void OnEndpointAdded(INetcodeSession session, PeerAddress address, NetcodePlayer player)
    {
        foreach (var plugin in plugins)
            plugin.OnEndpointAdded(session, address.EndPoint, player);
    }

    public void Dispose()
    {
        foreach (var plugin in plugins)
            try
            {
                plugin.Dispose();
            }
            catch (Exception e)
            {
                logger.Write(LogLevel.Error, $"plugin dispose error: {e}");
            }
    }
}

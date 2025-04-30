using Backdash.Network;
using Backdash.Options;

namespace Backdash.Core;

sealed class PluginManager(
    Logger logger,
    INetcodePlugin? plugin
) : IDisposable
{
    public void OnStart(INetcodeSession session)
    {
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
        try
        {
            plugin?.OnEndpointClosed(session, address.EndPoint, player);
        }
        catch (Exception e)
        {
            logger.Write(LogLevel.Error, $"plugin end-point close error: {e}");
        }
    }

    public void OnEndpointAdded(INetcodeSession session, PeerAddress address, NetcodePlayer player)
    {
        try
        {
            plugin?.OnEndpointAdded(session, address.EndPoint, player);
        }
        catch (Exception e)
        {
            logger.Write(LogLevel.Error, $"plugin end-point add error: {e}");
        }
    }

    public void OnFrameBegin(INetcodeSession session, bool isSynchronizing) =>
        plugin?.OnFrameBegin(session, isSynchronizing);

    public void Dispose() => plugin?.Dispose();
}

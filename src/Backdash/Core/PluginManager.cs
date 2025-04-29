using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Network;
using Backdash.Options;

namespace Backdash.Core;

sealed class PluginManager(
    Logger logger,
    IEnumerable<INetcodePlugin> plugins
) : IDisposable
{
    readonly INetcodePlugin[] plugins = plugins.ToArray();

    public void OnStart(INetcodeSession session)
    {
        foreach (var plugin in plugins)
            try
            {
                plugin.OnSessionStart(session);
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
                plugin.OnSessionClose(session);
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

    public void OnFrameBegin(INetcodeSession session, bool isSynchronizing)
    {
        if (plugins.Length is 0) return;
        ref var current = ref MemoryMarshal.GetReference(plugins.AsSpan());
        ref var limit = ref Unsafe.Add(ref current, plugins.Length);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            current.OnFrameBegin(session, isSynchronizing);
            current = ref Unsafe.Add(ref current, 1)!;
        }
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

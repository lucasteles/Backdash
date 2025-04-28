using System.Net;

namespace Backdash.Options;

/// <summary>
/// Netcode plugin to hook into the session lifetime
/// </summary>
public interface INetcodePlugin : IDisposable
{
    /// <summary>
    /// Session start hook
    /// </summary>
    void OnSessionStart(INetcodeSession session);

    /// <summary>
    /// Session close hook
    /// </summary>
    void OnSessionClose(INetcodeSession session);

    /// <summary>
    /// Start endpoint hook
    /// </summary>
    void OnEndpointAdded(INetcodeSession session, EndPoint endpoint, NetcodePlayer player);

    /// <summary>
    /// Close endpoint hook
    /// </summary>
    void OnEndpointClosed(INetcodeSession session, EndPoint endpoint, NetcodePlayer player);
}

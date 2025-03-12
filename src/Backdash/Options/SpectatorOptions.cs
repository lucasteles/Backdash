using System.Net;

namespace Backdash.Options;

/// <summary>
/// Configurations for <see cref="INetcodeSession{TInput}"/> in <see cref="SessionMode.Spectator"/> mode.
/// </summary>
public sealed record SpectatorOptions
{
    /// <summary>
    /// Host endpoint IP Address
    /// </summary>
    /// <value>Defaults to <see cref="IPAddress.Loopback"/> </value>
    public IPAddress HostAddress { get; set; } = IPAddress.Loopback;

    /// <summary>
    /// Host endpoint port
    /// </summary>
    /// <value>Defaults to 9000</value>
    public int HostPort { get; set; } = 9000;

    /// <summary>
    /// Host IP endpoint
    /// </summary>
    public IPEndPoint HostEndPoint
    {
        get => new(HostAddress, HostPort);
        set => (HostAddress, HostPort) = (value.Address, value.Port);
    }
}

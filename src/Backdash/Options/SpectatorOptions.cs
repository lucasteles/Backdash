using System.Net;

namespace Backdash.Options;

/// <summary>
///     Configurations for <see cref="INetcodeSession{TInput}" /> in <see cref="SessionMode.Spectator" /> mode.
/// </summary>
public sealed record SpectatorOptions
{
    /// <summary>
    ///     Host endpoint IP Address
    /// </summary>
    /// <value>Defaults to <see cref="IPAddress.Loopback" /> </value>
    public IPAddress HostAddress { get; set; } = IPAddress.Loopback;

    /// <summary>
    ///     Host endpoint port
    /// </summary>
    /// <value>Defaults to 9000</value>
    public int HostPort { get; set; } = 9000;

    /// <summary>
    ///     Host endpoint
    /// </summary>
    /// <value>Defaults to <see cref="IPEndPoint"/> using <see cref="HostAddress"/> and <see cref="HostPort"/></value>
    public EndPoint? HostEndPoint { get; set; }

    internal EndPoint GetHostEndPoint() => HostEndPoint ?? new IPEndPoint(HostAddress, HostPort);
}

using Backdash.Options;

namespace Backdash.Network.Client;

/// <summary>
///     Factory for peer sockets
/// </summary>
public interface IPeerSocketFactory
{
    /// <summary>
    ///     Creates instance of <see cref="IPeerSocket" />
    /// </summary>
    IPeerSocket Create(int port, NetcodeOptions options);
}

sealed class PeerSocketFactory : IPeerSocketFactory
{
    public IPeerSocket Create(int port, NetcodeOptions options) => new UdpSocket(port, options.UseIPv6);
}

sealed class DelegateSocketFactory(Func<int, NetcodeOptions, IPeerSocket> factory) : IPeerSocketFactory
{
    public IPeerSocket Create(int port, NetcodeOptions options) => factory.Invoke(port, options);
}

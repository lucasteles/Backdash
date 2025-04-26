using System.Threading.Channels;
using Backdash.Network.Protocol;

namespace Backdash.Network;

interface IProtocolNetworkEventHandler : IDisposable
{
    void OnNetworkEvent(in ProtocolEventInfo evt);
    void OnNetworkEvent(in ProtocolEvent evt, in PlayerHandle player) => OnNetworkEvent(new(evt, player));
}

sealed class ProtocolNetworkEventQueue : IProtocolNetworkEventHandler
{
    readonly Channel<ProtocolEventInfo> channel = Channel.CreateUnbounded<ProtocolEventInfo>(
        new()
        {
            SingleWriter = false,
            SingleReader = true,
            AllowSynchronousContinuations = true,
        });

    public bool TryRead(out ProtocolEventInfo nextEvent) => channel.Reader.TryRead(out nextEvent);
    public void OnNetworkEvent(in ProtocolEventInfo evt) => channel.Writer.TryWrite(evt);

    public void Dispose() => channel.Writer.Complete();
}

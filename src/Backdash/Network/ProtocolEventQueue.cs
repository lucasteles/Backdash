using System.Threading.Channels;
using Backdash.Network.Protocol;

namespace Backdash.Network;

interface IProtocolEventQueue : IDisposable
{
    Func<ProtocolEvent, bool> Router { get; set; }

    bool TryConsume(out ProtocolEvent nextEvent);

    void Publish(in ProtocolEvent evt);

    void Publish(in ProtocolEventType evt, in PlayerHandle player) =>
        Publish(new ProtocolEvent(evt, player));
}

sealed class ProtocolEventQueue : IProtocolEventQueue
{
    public Func<ProtocolEvent, bool> Router { get; set; } = delegate { return false; };
    bool disposed;

    readonly Channel<ProtocolEvent> channel = Channel.CreateUnbounded<ProtocolEvent>(
        new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true,
            AllowSynchronousContinuations = true,
        });

    public bool TryConsume(out ProtocolEvent nextEvent) => channel.Reader.TryRead(out nextEvent);

    public void Publish(in ProtocolEvent evt)
    {
        if (disposed) return;
        if (Router(evt)) return;
        channel.Writer.TryWrite(evt).AssertTrue();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        channel.Writer.Complete();
    }
}

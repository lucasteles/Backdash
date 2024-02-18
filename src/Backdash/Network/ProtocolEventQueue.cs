using System.Threading.Channels;
using Backdash.Network.Protocol;

namespace Backdash.Network;

interface IProtocolEventQueue<TInput> : IDisposable where TInput : struct
{
    Func<ProtocolEvent<TInput>, bool> Router { get; set; }

    bool TryConsume(out ProtocolEvent<TInput> nextEvent);

    void Publish(in ProtocolEvent<TInput> evt);

    void Publish(in ProtocolEventType evt, in PlayerHandle player) =>
        Publish(new ProtocolEvent<TInput>(evt, player));
}

sealed class ProtocolEventQueue<TInput> : IProtocolEventQueue<TInput> where TInput : struct
{
    public Func<ProtocolEvent<TInput>, bool> Router { get; set; } = delegate { return false; };
    bool disposed;

    readonly Channel<ProtocolEvent<TInput>> channel = Channel.CreateUnbounded<ProtocolEvent<TInput>>(
        new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true,
            AllowSynchronousContinuations = true,
        });

    public bool TryConsume(out ProtocolEvent<TInput> nextEvent) => channel.Reader.TryRead(out nextEvent);

    public void Publish(in ProtocolEvent<TInput> evt)
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

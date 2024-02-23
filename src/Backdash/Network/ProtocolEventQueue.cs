using System.Diagnostics;
using System.Threading.Channels;
using Backdash.Network.Protocol;

namespace Backdash.Network;

interface IProtocolEventQueue<TInput> : IDisposable where TInput : struct
{
    Func<ProtocolEventInfo<TInput>, bool> ProxyFilter { get; set; }

    bool TryConsume(out ProtocolEventInfo<TInput> nextEvent);

    void Publish(in ProtocolEventInfo<TInput> evt);

    void Publish(in ProtocolEvent evt, in PlayerHandle player) =>
        Publish(new ProtocolEventInfo<TInput>(evt, player));
}

sealed class ProtocolEventQueue<TInput> : IProtocolEventQueue<TInput> where TInput : struct
{
    public Func<ProtocolEventInfo<TInput>, bool> ProxyFilter { get; set; } = delegate { return false; };
    bool disposed;

    readonly Channel<ProtocolEventInfo<TInput>> channel = Channel.CreateUnbounded<ProtocolEventInfo<TInput>>(
        new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true,
            AllowSynchronousContinuations = true,
        });

    public bool TryConsume(out ProtocolEventInfo<TInput> nextEvent) => channel.Reader.TryRead(out nextEvent);

    public void Publish(in ProtocolEventInfo<TInput> evt)
    {
        if (disposed || ProxyFilter(evt)) return;
        var published = channel.Writer.TryWrite(evt);
        Trace.Assert(published);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        channel.Writer.Complete();
    }
}

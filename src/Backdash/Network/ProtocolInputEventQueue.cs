using System.Diagnostics;
using System.Threading.Channels;
using Backdash.Sync.Input;
using Backdash.Sync.Input.Spectator;

namespace Backdash.Network;

readonly record struct GameInputEvent<TInput>(PlayerHandle Player, GameInput<TInput> Input)
    where TInput : struct
{
    public readonly PlayerHandle Player = Player;
    public readonly GameInput<TInput> Input = Input;
}

interface IProtocolInputEventPublisher<TInput> where TInput : struct
{
    void Publish(in GameInputEvent<TInput> evt);
}

interface IProtocolInputEventConsumer<TInput> where TInput : struct
{
    bool TryConsume(out GameInputEvent<TInput> nextEvent);
}

interface IProtocolInputEventQueue<TInput> :
    IDisposable, IProtocolInputEventPublisher<TInput>, IProtocolInputEventConsumer<TInput>
    where TInput : struct;

sealed class ProtocolInputEventQueue<TInput> : IProtocolInputEventQueue<TInput> where TInput : struct
{
    bool disposed;

    readonly Channel<GameInputEvent<TInput>> channel = Channel.CreateUnbounded<GameInputEvent<TInput>>(
        new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true,
            AllowSynchronousContinuations = true,
        });

    public bool TryConsume(out GameInputEvent<TInput> nextEvent) => channel.Reader.TryRead(out nextEvent);

    public void Publish(in GameInputEvent<TInput> evt)
    {
        if (disposed) return;
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

sealed class ProtocolCombinedInputsEventPublisher<TInput>(IProtocolInputEventPublisher<TInput> peerInputEventPublisher)
    : IProtocolInputEventPublisher<CombinedInputs<TInput>>
    where TInput : struct
{
    public void Publish(in GameInputEvent<CombinedInputs<TInput>> evt)
    {
        var player = evt.Player;
        var frame = evt.Input.Frame;
        for (var i = 0; i < evt.Input.Data.Count; i++)
        {
            ref readonly var current = ref evt.Input.Data.Inputs[i];
            peerInputEventPublisher.Publish(
                new GameInputEvent<TInput>(
                    player,
                    new GameInput<TInput>(current, frame))
            );
        }
    }
}

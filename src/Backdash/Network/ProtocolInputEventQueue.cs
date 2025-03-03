using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Backdash.Core;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Input.Confirmed;

namespace Backdash.Network;

readonly record struct GameInputEvent<TInput>(PlayerHandle Player, GameInput<TInput> Input)
    where TInput : unmanaged
{
    public readonly PlayerHandle Player = Player;
    public readonly GameInput<TInput> Input = Input;
}

interface IProtocolInputEventPublisher<TInput> where TInput : unmanaged
{
    void Publish(in GameInputEvent<TInput> evt);
}

interface IProtocolInputEventConsumer<TInput> where TInput : unmanaged
{
    bool TryConsume(out GameInputEvent<TInput> nextEvent);
}

interface IProtocolInputEventQueue<TInput> :
    IDisposable, IProtocolInputEventPublisher<TInput>, IProtocolInputEventConsumer<TInput>
    where TInput : unmanaged;

sealed class ProtocolInputEventQueue<TInput> : IProtocolInputEventQueue<TInput> where TInput : unmanaged
{
    bool disposed;

    readonly Channel<GameInputEvent<TInput>> channel = Channel.CreateUnbounded<GameInputEvent<TInput>>(
        new()
        {
            SingleWriter = false,
            SingleReader = true,
            AllowSynchronousContinuations = true,
        });

    public bool TryConsume(out GameInputEvent<TInput> nextEvent) => channel.Reader.TryRead(out nextEvent);

    public void Publish(in GameInputEvent<TInput> evt)
    {
        if (disposed) return;
        ThrowIf.Assert(channel.Writer.TryWrite(evt));
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        channel.Writer.Complete();
    }
}

sealed class ProtocolCombinedInputsEventPublisher<TInput>(IProtocolInputEventPublisher<TInput> peerInputEventPublisher)
    : IProtocolInputEventPublisher<ConfirmedInputs<TInput>>
    where TInput : unmanaged
{
    public void Publish(in GameInputEvent<ConfirmedInputs<TInput>> evt)
    {
        var player = evt.Player;
        var frame = evt.Input.Frame;

        var span = evt.Input.Data.Inputs[..evt.Input.Data.Count];
        ref var pointer = ref MemoryMarshal.GetReference(span);
        ref var end = ref Unsafe.Add(ref pointer, span.Length);

        while (Unsafe.IsAddressLessThan(ref pointer, ref end))
        {
            peerInputEventPublisher.Publish(new(player, new(pointer, frame)));
            pointer = ref Unsafe.Add(ref pointer, 1)!;
        }
    }
}

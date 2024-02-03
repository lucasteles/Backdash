using nGGPO.Core;
using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Messages;

namespace nGGPO.Network.Protocol.Messaging;

interface IInputEncoder
{
    InputCompressor Compress(in GameInput lastAcked, ref InputMsg msg);
    InputDecompressor Decompress(ref InputMsg inputMsg, ref GameInput lastReceivedInput);
}

sealed class InputEncoder : IInputEncoder
{
    public InputCompressor Compress(in GameInput lastAcked, ref InputMsg msg) =>
        new(in lastAcked, ref msg);

    public InputDecompressor Decompress(ref InputMsg inputMsg, ref GameInput lastReceivedInput) =>
        new(ref inputMsg, ref lastReceivedInput);
}

ref struct InputCompressor
{
    BitOffsetWriter bitWriter;

    ref InputMsg inputMsg;

    public GameInput Last;

    public InputCompressor(
        in GameInput lastAcked,
        ref InputMsg msg
    )
    {
        Count = 0;
        Last = lastAcked;

        inputMsg = ref msg;
        bitWriter = new(msg.Bits);
        inputMsg.StartFrame = Frame.NullValue;
    }

    public int Count { get; private set; }

    public void WriteInput(in GameInput current)
    {
        Count++;
        if (inputMsg.StartFrame is Frame.NullValue)
        {
            inputMsg.InputSize = (byte)current.Size;
            inputMsg.StartFrame = current.Frame;
            Tracer.Assert(Last.Frame.IsNull || Last.Frame.Next == inputMsg.StartFrame);
        }

        if (!current.Equals(Last, bitsOnly: true))
        {
            var currentBits = ReadOnlyBitVector.FromSpan(current.Buffer);
            var lastBits = ReadOnlyBitVector.FromSpan(Last.Buffer);
            for (var i = 0; i < currentBits.BitCount; i++)
            {
                if (currentBits[i] == lastBits[i])
                    continue;

                bitWriter.SetNext();

                if (currentBits[i])
                    bitWriter.SetNext();
                else
                    bitWriter.ClearNext();

                bitWriter.WriteNibble(i);
            }
        }

        bitWriter.ClearNext();
        Last = current;

        inputMsg.NumBits = bitWriter.Offset;
        Tracer.Assert(inputMsg.NumBits < Max.CompressedBits);
    }
}

ref struct InputDecompressor
{
    readonly ushort numBits;
    readonly BitVector lastInputBits;
    ref GameInput nextInput;
    BitOffsetWriter bitVector;
    int currentFrame;

    public InputDecompressor(
        ref InputMsg inputMsg,
        ref GameInput lastReceivedInput
    )
    {
        numBits = inputMsg.NumBits;
        currentFrame = inputMsg.StartFrame;
        nextInput = ref lastReceivedInput;
        nextInput.Size = inputMsg.InputSize;

        if (lastReceivedInput.Frame < 0)
            lastReceivedInput.Frame = new(inputMsg.StartFrame - 1);

        lastInputBits = BitVector.FromSpan(nextInput.Buffer);
        bitVector = new(inputMsg.Bits);
    }

    public bool NextInput(ILogger? logger = null)
    {
        while (bitVector.Offset < numBits)
        {
            /*
             * Keep walking through the frames (parsing bits) until we reach
             * the inputs for the frame right after the one we're on.
             */
            var nextFrame = nextInput.Frame.Next;
            Tracer.Assert(currentFrame <= nextFrame);
            var useInputs = currentFrame == nextFrame;

            while (bitVector.Read())
            {
                var on = bitVector.Read();
                var button = bitVector.ReadNibble();
                if (!useInputs) continue;
                if (on)
                    lastInputBits.Set(button);
                else
                    lastInputBits.Clear(button);
            }

            Tracer.Assert(bitVector.Offset <= numBits);

            /*
             * Now if we want to use these inputs, go ahead and send them to
             * the emulator.
             */

            if (useInputs)
            {
                /*
                 * Move forward 1 frame in the stream.
                 */
                Tracer.Assert(currentFrame == nextInput.Frame.Next);
                nextInput.Frame = new(currentFrame);
                currentFrame++;
                return true;
            }

            logger?.Info($"Skipping past frame:{currentFrame} current is {nextInput.Frame}");

            /*
             * Move forward 1 frame in the input stream.
             */
            currentFrame++;
        }

        return false;
    }
}

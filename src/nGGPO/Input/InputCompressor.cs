using nGGPO.Data;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Input;

class InputCompressor
{
    public InputMsg Compress(
        in GameInput lastAcked,
        in CircularBuffer<GameInput> pendingOutput,
        ref GameInput lastSent
    )
    {
        ref var front = ref pendingOutput.Peek();

        InputMsg inputMsg = new()
        {
            InputSize = (byte)front.Size,
            StartFrame = front.Frame,
        };

        var last = lastAcked;
        var lastBits = last.GetBitVector();
        Span<byte> bits = inputMsg.Bits;

        BitVector.BitOffset bitWriter = new(bits);
        Tracer.Assert(last.Frame.IsNull || last.Frame.Next == inputMsg.StartFrame);

        for (var i = 0; i < pendingOutput.Size; i++)
        {
            ref var current = ref pendingOutput[i];

            if (!current.Equals(last, bitsOnly: true))
            {
                var currentBits = current.GetBitVector();
                for (var j = 0; j < currentBits.BitCount; j++)
                {
                    if (currentBits[j] == lastBits[j])
                        continue;

                    bitWriter.SetNext();

                    if (currentBits[j])
                        bitWriter.SetNext();
                    else
                        bitWriter.ClearNext();

                    bitWriter.WriteNibble(j);
                }
            }

            bitWriter.ClearNext();
            last = current;
            lastSent = current;
        }

        inputMsg.NumBits = (ushort)bitWriter.Offset;
        Tracer.Assert(inputMsg.NumBits < Max.CompressedBits);

        return inputMsg;
    }

    public void Decompress(
        ref InputMsg msg,
        ref GameInput lastReceivedInput,
        Action onParsedInput
    )
    {
        var numBits = msg.NumBits;
        var currentFrame = msg.StartFrame;

        lastReceivedInput.Size = msg.InputSize;

        if (lastReceivedInput.Frame < 0)
            lastReceivedInput.SetFrame(new(msg.StartFrame - 1));

        Span<byte> bits = msg.Bits;
        BitVector.BitOffset bitVector = new(bits);
        var lastInputBits = lastReceivedInput.GetBitVector();

        while (bitVector.Offset < numBits)
        {
            /*
             * Keep walking through the frames (parsing bits) until we reach
             * the inputs for the frame right after the one we're on.
             */
            Tracer.Assert(currentFrame <= lastReceivedInput.Frame.Next);
            var useInputs = currentFrame == lastReceivedInput.Frame.Next;

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
                Tracer.Assert(currentFrame == lastReceivedInput.Frame.Next);
                lastReceivedInput.SetFrame(new(currentFrame));

                onParsedInput();
            }
            else
            {
                Tracer.Log("Skipping past frame:(%d) current is %d.\n",
                    currentFrame, lastReceivedInput.Frame);
            }

            /*
             * Move forward 1 frame in the input stream.
             */
            currentFrame++;
        }
    }
}

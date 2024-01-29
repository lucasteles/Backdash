using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol.Internal;

sealed class InputEncoder
{
    public static InputMsg Compress(
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
        BitVector.BitOffset bitWriter = new(inputMsg.Bits);
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

    public static DecompressReader Decompress(ref InputMsg inputMsg, ref GameInput lastReceivedInput) =>
        new(ref inputMsg, ref lastReceivedInput);

    public ref struct DecompressReader
    {
        readonly ushort numBits;
        readonly BitVector lastInputBits;
        ref GameInput nextInput;
        BitVector.BitOffset bitVector;
        int currentFrame;

        public DecompressReader(
            ref InputMsg inputMsg,
            ref GameInput lastReceivedInput
        )
        {
            numBits = inputMsg.NumBits;
            currentFrame = inputMsg.StartFrame;
            nextInput = ref lastReceivedInput;
            nextInput.Size = inputMsg.InputSize;

            if (lastReceivedInput.Frame < 0)
                lastReceivedInput.SetFrame(new(inputMsg.StartFrame - 1));

            lastInputBits = nextInput.GetBitVector();

            Span<byte> bits = inputMsg.Bits;
            bitVector = new(bits);
        }

        public bool NextInput()
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
                    nextInput.SetFrame(new(currentFrame));
                    currentFrame++;
                    return true;
                }

                Tracer.Log("Skipping past frame:(%d) current is %d.\n", currentFrame, nextInput.Frame);

                /*
                 * Move forward 1 frame in the input stream.
                 */
                currentFrame++;
            }

            return false;
        }
    }
}

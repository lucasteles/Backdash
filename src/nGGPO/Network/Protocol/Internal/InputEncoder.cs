using System.Threading.Channels;
using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol.Internal;

sealed class InputEncoder
{
    public static InputMsg Compress(
        ChannelReader<GameInput> pendingReader,
        in GameInput lastAcked,
        ref GameInput lastSent,
        out int counter
    )
    {
        counter = 0;
        InputMsg inputMsg = new();
        var last = lastAcked;
        var lastBits = last.GetBitVector();
        BitVector.BitOffset bitWriter = new(inputMsg.Bits);

        var first = true;
        while (pendingReader.TryRead(out var current))
        {
            counter++;
            if (first)
            {
                inputMsg.InputSize = (byte)current.Size;
                inputMsg.StartFrame = current.Frame;
                Tracer.Assert(last.Frame.IsNull || last.Frame.Next == inputMsg.StartFrame);
                first = false;
            }

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

    public static CompressorReader Decompress(ref InputMsg inputMsg, ref GameInput lastReceivedInput) =>
        new(ref inputMsg, ref lastReceivedInput);


    public ref struct CompressorWriter
    {
        readonly BitVector lastBits;

        ref GameInput last;
        ref GameInput lastSent;
        ref InputMsg inputMsg;
        BitVector.BitOffset bitWriter;
        int counter;

        public int Count => counter;

        public CompressorWriter(
            ref GameInput lastAcked,
            ref GameInput lastSent,
            ref InputMsg msg
        )
        {
            counter = 0;
            last = ref lastAcked;
            this.lastSent = ref lastSent;
            lastBits = last.GetBitVector();
            inputMsg = ref msg;
            bitWriter = new(msg.Bits);

            msg.StartFrame = Frame.NullValue;
        }

        public void WriteInput(ref GameInput current)
        {
            counter++;
            if (inputMsg.StartFrame is Frame.NullValue)
            {
                inputMsg.InputSize = (byte)current.Size;
                inputMsg.StartFrame = current.Frame;
                Tracer.Assert(last.Frame.IsNull || last.Frame.Next == inputMsg.StartFrame);
            }

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


            inputMsg.NumBits = (ushort)bitWriter.Offset;
            Tracer.Assert(inputMsg.NumBits < Max.CompressedBits);
        }
    }

    public ref struct CompressorReader
    {
        readonly ushort numBits;
        readonly BitVector lastInputBits;
        ref GameInput nextInput;
        BitVector.BitOffset bitVector;
        int currentFrame;

        public CompressorReader(
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

using System.Threading.Channels;
using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol.Internal;

sealed class InputEncoder
{
    public static Compressor Compress(in GameInput lastAcked, ref InputMsg msg) =>
        new(in lastAcked, ref msg);

    public static Decompressor Decompress(ref InputMsg inputMsg, ref GameInput lastReceivedInput) =>
        new(ref inputMsg, ref lastReceivedInput);


    public ref struct Compressor
    {
        BitVector.BitOffset bitWriter;

        ref InputMsg inputMsg;

        public GameInput Last;

        public Compressor(
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
                var currentBits = current.GetReadOnlyBitVector();
                var lastBits = Last.GetReadOnlyBitVector();
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

    public ref struct Decompressor
    {
        readonly ushort numBits;
        readonly BitVector lastInputBits;
        ref GameInput nextInput;
        BitVector.BitOffset bitVector;
        int currentFrame;

        public Decompressor(
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

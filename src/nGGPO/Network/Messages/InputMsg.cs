using System.Runtime.CompilerServices;
using System.Text;
using nGGPO.Input;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

namespace nGGPO.Network.Messages;

[Serializable]
record struct InputMsg : IBinarySerializable
{
    public byte PeerCount;
    public PeerStatusBuffer PeerConnectStatus;
    public int StartFrame;
    public bool DisconnectRequested;
    public int AckFrame;
    public ushort NumBits;
    public byte InputSize;
    public GameInputBuffer Bits;

    public static readonly InputMsg Empty = new();

    public readonly void Serialize(scoped NetworkBufferWriter writer)
    {
        writer.Write(PeerCount);
        for (var i = 0; i < PeerCount; i++)
            PeerConnectStatus[i].Serialize(writer);

        writer.Write(StartFrame);
        writer.Write(DisconnectRequested);
        writer.Write(AckFrame);
        writer.Write(NumBits);
        writer.Write(InputSize);
        writer.Write(Bits, InputSize);
    }

    public void Deserialize(scoped NetworkBufferReader reader)
    {
        PeerCount = reader.ReadByte();
        for (var i = 0; i < PeerCount; i++)
            PeerConnectStatus[i].Deserialize(reader);

        StartFrame = reader.ReadInt();
        DisconnectRequested = reader.ReadBool();
        AckFrame = reader.ReadInt();
        NumBits = reader.ReadUShort();
        InputSize = reader.ReadByte();

        reader.ReadByte(Bits, InputSize);
    }
}

[Serializable, InlineArray(Max.MsgPlayers)]
struct PeerStatusBuffer
{
    ConnectStatus element0;

    public PeerStatusBuffer(ReadOnlySpan<ConnectStatus> buffer) => buffer.CopyTo(this);

    public override readonly string ToString()
    {
        ReadOnlySpan<ConnectStatus> values = this;
        StringBuilder builder = new();
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                builder.Append(',');

            var curr = values[i];

            builder.Append(curr.Disconnected ? "ON" : "OFF");
            builder.Append('(');
            builder.Append(curr.LastFrame);
            builder.Append(')');
            builder.Append(' ');
        }

        return builder.ToString();
    }
}

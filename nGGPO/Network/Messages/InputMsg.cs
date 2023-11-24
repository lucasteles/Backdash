using System.Runtime.CompilerServices;
using nGGPO.Input;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

namespace nGGPO.Network.Messages;

[InlineArray(Max.MsgPlayers)]
public struct PeerStatusBuffer
{
    ConnectStatus element0;
}

struct InputMsg
{
    public byte PeerCount;
    public PeerStatusBuffer PeerConnectStatus;
    public int StartFrame;
    public bool DisconnectRequested;
    public int AckFrame;
    public ushort NumBits;
    public byte InputSize;
    public GameInputBuffer Bits;

    public void Serialize(scoped NetworkBufferWriter writer)
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
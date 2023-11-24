using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using nGGPO.Input;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

namespace nGGPO.Network.Messages;

[InlineArray(Max.Players)]
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

    [Pure]
    public int PacketSize() =>
        sizeof(byte)
        + ConnectStatus.Size * PeerCount
        + sizeof(int)
        + sizeof(bool)
        + sizeof(int)
        + sizeof(ushort)
        + sizeof(byte)
        + sizeof(byte)
        + sizeof(byte) * InputSize;

    public class Serializer : BinarySerializer<InputMsg>
    {
        public static readonly Serializer Instance = new();

        public override int SizeOf(in InputMsg data) => data.PacketSize();

        protected internal override void Serialize(
            ref NetworkBufferWriter writer, in InputMsg data)
        {
            writer.Write(data.PeerCount);
            var statuses = data.PeerConnectStatus;
            for (var i = 0; i < data.PeerCount; i++)
                ConnectStatus.Serializer.Instance
                    .Serialize(ref writer, in statuses[i]);

            writer.Write(data.StartFrame);
            writer.Write(data.DisconnectRequested);
            writer.Write(data.AckFrame);
            writer.Write(data.NumBits);
            writer.Write(data.InputSize);

            var bits = Mem.InlineArrayAsReadOnlySpan<GameInputBuffer, byte>(
                in data.Bits, data.InputSize);
            writer.Write(bits);
        }

        protected internal override InputMsg Deserialize(ref NetworkBufferReader reader)
        {
            var statusLength = reader.ReadByte();
            PeerStatusBuffer peerStatus = new();
            for (var i = 0; i < statusLength; i++)
                peerStatus[i] =
                    ConnectStatus.Serializer.Instance.Deserialize(ref reader);

            var startFrame = reader.ReadInt();
            var disconnectRequested = reader.ReadBool();
            var ackFrame = reader.ReadInt();
            var numBits = reader.ReadUShort();
            var inputSize = reader.ReadByte();

            var input = new InputMsg
            {
                StartFrame = startFrame,
                PeerCount = statusLength,
                PeerConnectStatus = peerStatus,
                DisconnectRequested = disconnectRequested,
                AckFrame = ackFrame,
                NumBits = numBits,
                InputSize = inputSize,
                Bits = new(),
            };

            var bits = Mem.InlineArrayAsSpan<GameInputBuffer, byte>(ref input.Bits, inputSize);
            reader.ReadByte(in bits);

            return input;
        }
    }
}
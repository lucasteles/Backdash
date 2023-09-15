using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

namespace nGGPO.Network.Messages;

struct InputMsg : IDisposable
{
    public Memory<ConnectStatus> PeerConnectStatus;
    public int StartFrame;
    public bool DisconnectRequested;
    public int AckFrame;
    public ushort NumBits;
    public byte InputSize;
    public Memory<byte> Bits;

    readonly IMemoryOwner<byte>? bitsOwner;
    readonly IMemoryOwner<ConnectStatus> peerConnectStatusOwner;

    public InputMsg(
        IMemoryOwner<ConnectStatus> peerConnectStatusOwner,
        IMemoryOwner<byte>? bitsOwner
    )
    {
        this.peerConnectStatusOwner = peerConnectStatusOwner;
        this.bitsOwner = bitsOwner;
        PeerConnectStatus = peerConnectStatusOwner.Memory;
        Bits = bitsOwner?.Memory ?? Memory<byte>.Empty;

        StartFrame = default;
        DisconnectRequested = default;
        AckFrame = default;
        NumBits = default;
        InputSize = default;
    }

    public InputMsg(int peerCount, int bitsSize = 0) :
        this(
            Mem.Rent<ConnectStatus>(peerCount),
            bitsSize is 0 ? null : Mem.Rent(bitsSize)
        )
    {
    }

    public void Dispose()
    {
        peerConnectStatusOwner.Dispose();
        bitsOwner?.Dispose();
    }

    [Pure]
    public int PacketSize() =>
        sizeof(byte) // PeerConnectStatus size
        + ConnectStatus.Size * PeerConnectStatus.Length
        + sizeof(int)
        + sizeof(bool)
        + sizeof(int)
        + sizeof(ushort)
        + sizeof(byte)
        + sizeof(byte) // Bits size
        + sizeof(byte) * Bits.Length;

    public class Serializer : BinarySerializer<InputMsg>
    {
        public static readonly Serializer Instance = new();

        public override int SizeOf(in InputMsg data) => data.PacketSize();

        protected internal override void Serialize(
            ref NetworkBufferWriter writer, in InputMsg data)
        {
            writer.Write((byte) data.PeerConnectStatus.Length);
            for (var i = 0; i < data.PeerConnectStatus.Length; i++)
                ConnectStatus.Serializer.Instance
                    .Serialize(ref writer, in data.PeerConnectStatus.Span[i]);

            writer.Write(data.StartFrame);
            writer.Write(data.DisconnectRequested);
            writer.Write(data.AckFrame);
            writer.Write(data.NumBits);
            writer.Write(data.InputSize);
            writer.Write((byte) data.Bits.Length);
            writer.Write(data.Bits);
        }

        protected internal override InputMsg Deserialize(ref NetworkBufferReader reader)
        {
            var statusLength = reader.ReadByte();
            var peerStatus = Mem.Rent<ConnectStatus>(statusLength);
            for (var i = 0; i < statusLength; i++)
                peerStatus.Memory.Span[i] =
                    ConnectStatus.Serializer.Instance.Deserialize(ref reader);

            var startFrame = reader.ReadInt();
            var disconnectRequested = reader.ReadBool();
            var ackFrame = reader.ReadInt();
            var numBits = reader.ReadUShort();
            var inputSize = reader.ReadByte();

            var bitsLength = reader.ReadByte();
            var bits = Mem.Rent(bitsLength);
            reader.ReadMemory(bits.Memory);

            return new(peerStatus, bits)
            {
                StartFrame = startFrame,
                DisconnectRequested = disconnectRequested,
                AckFrame = ackFrame,
                NumBits = numBits,
                InputSize = inputSize,
            };
        }
    }
}
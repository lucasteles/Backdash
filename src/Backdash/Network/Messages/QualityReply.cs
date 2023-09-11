using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct QualityReply : IBinarySerializable
{
    public uint Pong;

    public readonly void Serialize(NetworkBufferWriter writer) =>
        writer.Write(Pong);

    public void Deserialize(NetworkBufferReader reader) =>
        Pong = reader.ReadUInt();
}

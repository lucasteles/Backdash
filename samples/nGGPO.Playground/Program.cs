using System.Buffers;
using nGGPO.Core;
using nGGPO.Inputs;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol.Messaging;
using nGGPO.Playground;
using nGGPO.Serialization;

void Div() => Console.WriteLine(new string('-', 10));
var pool = ArrayPool<byte>.Shared;

byte[] data =
{
    1, 2, 3, 4, 5,
};
{
    var packet = long.MaxValue;
    var serializer = BinarySerializerFactory.Get<long>()!;
    var buffer = pool.Rent(10);
    var size = serializer.Serialize(ref packet, buffer);
    var bytes = buffer[..size];
    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    Console.WriteLine($"# Pkg={backPacket}\n");
    pool.Return(buffer, true);
}
Div();
{
    var packet = ButtonsInput.UpLeft | ButtonsInput.X;
    var serializer = BinarySerializerFactory.Get<ButtonsInput>()!;
    var buffer = pool.Rent(10);
    var size = serializer.Serialize(ref packet, buffer);
    var bytes = buffer[..size];
    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    var buttons = new ButtonsInputEditor(backPacket);
    Console.WriteLine($"# Pkg= {buttons}\n");
    pool.Return(buffer, true);
}
Div();
{
    Input packet = new()
    {
        S = data.Length,
        A = (byte) 'a',
        B = 2,
        Bits = new(),
    };
    data.CopyTo(packet.Bits);
    Console.WriteLine($"# Ipt: {packet}\n");


    var serializer = BinarySerializerFactory.Get<Input>()!;
    var buffer = pool.Rent(20);
    var size = serializer.Serialize(ref packet, buffer);
    var bytes = buffer[..size];

    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    Console.WriteLine($"# Pkg: {backPacket}\n");
    pool.Return(buffer, true);
}
Div();
{
    Input packet = new()
    {
        S = data.Length,
        A = (byte) 'a',
        B = 2,
        Bits = new(),
    };
    data.CopyTo(packet.Bits);

    var serializer = new CustomInputSerializer
    {
        Network = false,
    };

    var buffer = pool.Rent(20);
    var size = serializer.Serialize(ref packet, buffer);
    var bytes = buffer[..size];

    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    Console.WriteLine($"# Pkg={backPacket}\n");
    pool.Return(buffer, true);
}

Div();
{
    ProtocolMessage packet = new()
    {
        Header = new(MsgType.SyncRequest)
        {
            Magic = 42,
            SequenceNumber = 10,
        },
        SyncRequest = new()
        {
            // RandomRequest = 99,
            RemoteMagic = 24,
            RemoteEndpoint = 128,
        },
    };
    packet.Header.Dump();
    packet.SyncRequest.Dump();

    var serializer = new ProtocolMessageBinarySerializer
    {
        Network = false,
    };
    var buffer = pool.Rent(Max.UdpPacketSize);
    var size = serializer.Serialize(ref packet, buffer);
    var bytes = buffer[..size];

    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    Console.WriteLine("# Pkg=\n");
    backPacket.Header.Dump();
    backPacket.SyncRequest.Dump();
    pool.Return(buffer, true);
}
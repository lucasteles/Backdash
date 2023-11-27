using nGGPO.Playground;
using nGGPO.Inputs;
using nGGPO.Network;
using nGGPO.Network.Messages;
using nGGPO.Serialization;
using nGGPO.Utils;

void Div() => Console.WriteLine(new string('-', 10));

byte[] data = {1, 2, 3, 4, 5};
{
    var packet = long.MaxValue;
    var serializer = BinarySerializers.Get<long>()!;
    using var buffer = MemoryBuffer.Rent(10, true);
    var size = serializer.Serialize(ref packet, buffer);
    var bytes = buffer[..size];

    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    Console.WriteLine($"# Pkg={backPacket}\n");
}
Div();
{
    var packet = ButtonsInput.UpLeft | ButtonsInput.X;
    var serializer = BinarySerializers.Get<ButtonsInput>()!;
    using var buffer = MemoryBuffer.Rent(10, true);
    var size = serializer.Serialize(ref packet, buffer);
    var bytes = buffer[..size];
    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    var buttons = new ButtonsInputEditor(backPacket);
    Console.WriteLine($"# Pkg= {buttons}\n");
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


    var serializer = BinarySerializers.Get<Input>()!;
    using var buffer = MemoryBuffer.Rent(20, true);
    var size = serializer.Serialize(ref packet, buffer);
    var bytes = buffer[..size];

    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    Console.WriteLine($"# Pkg: {backPacket}\n");
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

    var serializer = new CustomInputSerializer {Network = false};

    using var buffer = MemoryBuffer.Rent(20, true);
    var size = serializer.Serialize(ref packet, buffer);
    var bytes = buffer[..size];

    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    Console.WriteLine($"# Pkg={backPacket}\n");
}

Div();
{
    UdpMsg packet = new()
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

    var serializer = new UdpMsgBinarySerializer {Network = false};
    using var buffer = MemoryBuffer.Rent(Max.UdpPacketSize, true);

    var size = serializer.Serialize(ref packet, buffer);
    var bytes = buffer[..size];

    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    Console.WriteLine("# Pkg=\n");
    backPacket.Header.Dump();
    backPacket.SyncRequest.Dump();
}
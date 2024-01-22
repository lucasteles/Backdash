using System.Net;
using nGGPO.Network;
using nGGPO.Serialization;

Console.WriteLine("Starting...");

UdpPeerClient<Message> peer1 = new(9000, BinarySerializers.ForEnum<Message>());
peer1.OnMessage += async (message, sender, token) =>
{
    if (token.IsCancellationRequested)
        return;

    switch (message)
    {
        case Message.Ping:
            await peer1.SendTo(sender, Message.Pong, token);
            break;
        case Message.Pong:
            await peer1.SendTo(sender, Message.Ping, token);
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(message), message, null);
    }
};

UdpPeerClient<Message> peer2 = new(9001, BinarySerializers.ForEnum<Message>());
peer2.OnMessage += async (message, sender, token) =>
{
    if (token.IsCancellationRequested)
        return;

    switch (message)
    {
        case Message.Ping:
            await peer2.SendTo(sender, Message.Pong, token);
            break;
        case Message.Pong:
            await peer2.SendTo(sender, Message.Ping, token);
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(message), message, null);
    }
};


CancellationTokenSource source = new();
var tasks = Task.WhenAll(peer1.Start(source.Token), peer2.Start(source.Token));

var address2 = new IPEndPoint(IPAddress.Loopback, 9001).Serialize();
await peer1.SendTo(address2, Message.Ping, source.Token);

Console.ReadLine();

await source.CancelAsync();
await tasks;
Console.WriteLine("Ending...");

public enum Message : short
{
    Ping = 2,
    Pong = 4
}

using System.Net;
using nGGPO.Network.Client;
using nGGPO.Serialization;

#pragma warning disable AsyncFixer01

namespace nGGPO.Tests.Specs.Network;

public class UdpPeerClientTests
{
    [Fact]
    public async Task ShouldSend()
    {
        using Peer2PeerFixture<StringValue> context = new(new StringBinarySerializer());
        var (client, server) = context;

        StringValue msg = "hello server";
        SemaphoreSlim sem = new(0, 1);

        server.Observer.OnMessage += (_, message, sender, token) =>
        {
            message.Value.Should().Be("hello server");
            sender.Should().Be(client.Address);
            sem.Release();

            return ValueTask.CompletedTask;
        };

        await client.Socket.SendTo(server.Address, msg);
        var pass = await sem.WaitAsync(TimeSpan.FromSeconds(1));
        pass.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldSendAndReceive()
    {
        using Peer2PeerFixture<StringValue> context = new(new StringBinarySerializer());
        var (client, server) = context;

        AsyncCounter counter = new();

        server.Observer.OnMessage += async (_, message, sender, token) =>
        {
            message.Value.Should().Be("hello server");
            sender.Should().Be(client.Address);
            counter.Inc();
            await server.Socket.SendTo(sender, "hello client", token);
        };

        client.Observer.OnMessage += (_, message, sender, token) =>
        {
            message.Value.Should().Be("hello client");
            sender.Should().Be(server.Address);
            counter.Inc();
            return ValueTask.CompletedTask;
        };

        await client.Socket.SendTo(server.Address, "hello server");

        await WaitFor.BeTrue(() => counter.Value is 2);
    }

    enum OpMessage : short
    {
        Increment = 1,
        Decrement = 2,
        IncrementCallback = 3,
        DecrementCallback = 4,
    }

    void HandleMessage(ref int totalResult, OpMessage message)
    {
        switch (message)
        {
            case OpMessage.Increment:
                Interlocked.Increment(ref totalResult);
                break;
            case OpMessage.Decrement:
                Interlocked.Decrement(ref totalResult);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message), message, null);
        }
    }

    [Fact]
    public async Task ShouldProcessConcurrent()
    {
        using Peer2PeerFixture<OpMessage> context = new(BinarySerializerFactory.ForEnum<OpMessage>());
        var (client, server) = context;

        var totalResult = 0;
        AsyncCounter counter = new();

        server.Observer.OnMessage += (_, message, sender, token) =>
        {
            sender.Should().Be(client.Address);
            HandleMessage(ref totalResult, message);
            counter.Inc();
            return ValueTask.CompletedTask;
        };

        client.Observer.OnMessage += (_, message, sender, token) =>
        {
            sender.Should().Be(server.Address);
            HandleMessage(ref totalResult, message);
            counter.Inc();
            return ValueTask.CompletedTask;
        };

        const int messageCount = 100_000;
        Random rnd = new(42);

        await Task.WhenAll(Enumerable.Range(0, messageCount).Select(i => Task.Run(async () =>
        {
            var msg = i % 2 is 0 ? OpMessage.Increment : OpMessage.Decrement;

            if (rnd.Next() % 2 is 0)
                await client.Socket.SendTo(server.Address, msg);
            else
                await server.Socket.SendTo(client.Address, msg);
        })));

        await WaitFor.BeTrue(() => counter.Value is messageCount);
        totalResult.Should().Be(0);
    }

    [Fact]
    public async Task ShouldSendReceiveBetween()
    {
        using Peer2PeerFixture<OpMessage> context = new(
            BinarySerializerFactory.ForEnum<OpMessage>()
        );

        var (client, server) = context;

        var totalResult = 0;
        AsyncCounter counter = new();
        server.Observer.OnMessage += async (_, message, sender, token) =>
        {
            sender.Should().Be(client.Address);
            await HandleMessageAsync(message, server.Socket, sender, token);
            counter.Inc();
        };

        client.Observer.OnMessage += async (_, message, sender, token) =>
        {
            sender.Should().Be(server.Address);
            await HandleMessageAsync(message, client.Socket, sender, token);
            counter.Inc();
        };

        const int messageCount = 50_000;
        Random rnd = new(42);

        var tasks = Task.WhenAll(Enumerable.Range(0, messageCount).Select(i => Task.Run(async () =>
        {
            var msg = i % 2 is 0
                ? OpMessage.IncrementCallback
                : OpMessage.DecrementCallback;

            if (rnd.Next() % 2 is 0)
                await client.Socket.SendTo(server.Address, msg);
            else
                await server.Socket.SendTo(client.Address, msg);
        })));

        await WaitFor.BeTrue(() => counter.Value is messageCount * 2);
        await tasks;
        totalResult.Should().Be(0);

        return;

        async ValueTask HandleMessageAsync(
            OpMessage message,
            UdpPeerClient<OpMessage> udpClient,
            SocketAddress sender,
            CancellationToken ct
        )
        {
            switch (message)
            {
                case OpMessage.Increment or OpMessage.Decrement:
                    HandleMessage(ref totalResult, message);
                    break;
                case OpMessage.IncrementCallback:
                    Interlocked.Increment(ref totalResult);
                    await udpClient.SendTo(sender, OpMessage.Decrement, ct);
                    break;
                case OpMessage.DecrementCallback:
                    Interlocked.Decrement(ref totalResult);
                    await udpClient.SendTo(sender, OpMessage.Increment, ct);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message), message, null);
            }
        }
    }
}

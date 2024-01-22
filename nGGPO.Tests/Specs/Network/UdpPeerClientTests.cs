using nGGPO.Serialization;

#pragma warning disable AsyncFixer01

namespace nGGPO.Tests.UdpPeerClientTests;

public class UdpPeerClientTests
{
    [Fact]
    public async Task ShouldSend()
    {
        using Peer2PeerFixture<StringValue> context = new(new StringBinarySerializer());
        var (client, server) = context;

        StringValue msg = "hello server";
        SemaphoreSlim sem = new(0, 1);

        server.Socket.OnMessage += (message, sender, token) =>
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

        var totalProcessed = 0;

        server.Socket.OnMessage += async (message, sender, token) =>
        {
            message.Value.Should().Be("hello server");
            sender.Should().Be(client.Address);
            Interlocked.Increment(ref totalProcessed);
            await server.Socket.SendTo(sender, "hello client", token);
        };

        client.Socket.OnMessage += (message, sender, token) =>
        {
            message.Value.Should().Be("hello client");
            sender.Should().Be(server.Address);
            Interlocked.Increment(ref totalProcessed);
            return ValueTask.CompletedTask;
        };

        await client.Socket.SendTo(server.Address, "hello server");

        await WaitFor.BeTrue(() => totalProcessed is 2);
    }

    public enum OpMessage : short
    {
        Increment = 1,
        Decrement = 2,
    }

    [Fact]
    public async Task ShouldProcessConcurrent()
    {
        using Peer2PeerFixture<OpMessage> context = new(BinarySerializers.ForEnum<OpMessage>());
        var (client, server) = context;

        var totalResult = 0;
        AsyncCounter counter = new();

        server.Socket.OnMessage += (message, sender, token) =>
        {
            sender.Should().Be(client.Address);
            HandleMessage(message);
            counter.Inc();
            return ValueTask.CompletedTask;
        };

        client.Socket.OnMessage += (message, sender, token) =>
        {
            sender.Should().Be(server.Address);
            HandleMessage(message);
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

        return;

        void HandleMessage(OpMessage message)
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
    }
}

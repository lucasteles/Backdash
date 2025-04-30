using System.Net;
using Backdash.Network.Client;
using Backdash.Serialization.Internal;
using Backdash.Tests.TestUtils;
using Backdash.Tests.TestUtils.Fixtures;
using Backdash.Tests.TestUtils.Network;
using Backdash.Tests.TestUtils.Types;

#pragma warning disable AsyncFixer01
namespace Backdash.Tests.Specs.Integration.Network;

public class PeerClientTests
{
    [Fact]
    public async Task ShouldSend()
    {
        using Peer2PeerFixture<StringValue> context = new(new StringBinarySerializer());
        var (client, server) = context;
        StringValue msg = "hello server";
        SemaphoreSlim sem = new(0, 1);
        server.Observer.OnMessage += (message, sender, _) =>
        {
            message.Value.Should().Be("hello server");
            sender.Should().Be(client.Address);
            sem.Release();
        };
        await client.Client.SendTo(server.Address, msg);
        var pass = await sem.WaitAsync(TimeSpan.FromSeconds(1));
        pass.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldSendAndReceive()
    {
        using Peer2PeerFixture<StringValue> context = new(new StringBinarySerializer());
        var (client, server) = context;
        AsyncCounter counter = new();
        server.Observer.OnMessage += (message, sender, _) =>
        {
            message.Value.Should().Be("hello server");
            sender.Should().Be(client.Address);
            counter.Inc();
            server.Client.TrySendTo(sender, "hello client", null);
        };
        client.Observer.OnMessage += (message, sender, _) =>
        {
            message.Value.Should().Be("hello client");
            sender.Should().Be(server.Address);
            counter.Inc();
        };
        await client.Client.SendTo(server.Address, "hello server");
        await WaitFor.BeTrue(() => counter.Value is 2);
    }

    enum OpMessage : short
    {
        Increment = 1,
        Decrement = 2,
        IncrementCallback = 3,
        DecrementCallback = 4,
    }

    static void HandleMessage(ref int totalResult, OpMessage message)
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

    [Fact(Skip = "permission denied on CI")]
    public async Task ShouldProcessConcurrent()
    {
        using Peer2PeerFixture<OpMessage> context = new(BinarySerializerFactory.ForEnum<OpMessage>());
        var (client, server) = context;
        var totalResult = 0;
        AsyncCounter counter = new();
        server.Observer.OnMessage += (message, sender, _) =>
        {
            sender.Should().Be(client.Address);
            HandleMessage(ref totalResult, message);
            counter.Inc();
        };
        client.Observer.OnMessage += (message, sender, _) =>
        {
            sender.Should().Be(server.Address);
            HandleMessage(ref totalResult, message);
            counter.Inc();
        };
        var messageCount = 100;
        Random rnd = new(42);
        await Task.WhenAll(Enumerable.Range(0, messageCount).Select(i => Task.Run(async () =>
        {
            var msg = i % 2 is 0 ? OpMessage.Increment : OpMessage.Decrement;
            if (rnd.Next() % 2 is 0)
                await client.Client.SendTo(server.Address, msg);
            else
                await server.Client.SendTo(client.Address, msg);
        })));
        await WaitFor.BeTrue(
            () => counter.Value == messageCount,
            TimeSpan.FromSeconds(2),
            $"{counter.Value} != {messageCount}"
        );
        totalResult.Should().Be(0);
    }

    [Fact(Skip = "permission denied on CI")]
    public async Task ShouldSendReceiveBetween()
    {
        using Peer2PeerFixture<OpMessage> context = new(
            BinarySerializerFactory.ForEnum<OpMessage>()
        );
        var (client, server) = context;
        var totalResult = 0;
        AsyncCounter counter = new();
        server.Observer.OnMessage += (message, sender, _) =>
        {
            sender.Should().Be(client.Address);
            HandleMessageAsync(message, server.Client, sender);
            counter.Inc();
        };
        client.Observer.OnMessage += (message, sender, _) =>
        {
            sender.Should().Be(server.Address);
            HandleMessageAsync(message, client.Client, sender);
            counter.Inc();
        };
        var messageCount = 100;
        Random rnd = new(42);
        var tasks = Task.WhenAll(Enumerable.Range(0, messageCount).Select(i => Task.Run(async () =>
        {
            var msg = i % 2 is 0
                ? OpMessage.IncrementCallback
                : OpMessage.DecrementCallback;
            if (rnd.Next() % 2 is 0)
                await client.Client.SendTo(server.Address, msg);
            else
                await server.Client.SendTo(client.Address, msg);
        })));
        await WaitFor.BeTrue(() => counter.Value == messageCount * 2);
        await tasks;
        totalResult.Should().Be(0);
        return;

        void HandleMessageAsync(
            OpMessage message,
            PeerClient<OpMessage> udpClient,
            SocketAddress sender
        )
        {
            switch (message)
            {
                case OpMessage.Increment or OpMessage.Decrement:
                    HandleMessage(ref totalResult, message);
                    break;
                case OpMessage.IncrementCallback:
                    Interlocked.Increment(ref totalResult);
                    Assert.True(udpClient.TrySendTo(sender, OpMessage.Decrement));
                    break;
                case OpMessage.DecrementCallback:
                    Interlocked.Decrement(ref totalResult);
                    Assert.True(udpClient.TrySendTo(sender, OpMessage.Increment));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message), message, null);
            }
        }
    }
}

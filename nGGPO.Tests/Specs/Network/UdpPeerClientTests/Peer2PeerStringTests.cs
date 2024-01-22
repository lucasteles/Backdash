#pragma warning disable AsyncFixer01

namespace nGGPO.Tests.UdpPeerClientTests;

[Collection(TestCollection.Network)]
public class Peer2PeerStringTests(
    Peer2PeerFixture<StringValue> context
) : IClassFixture<Peer2PeerFixture<StringValue>>
{
    [Fact]
    public async Task ShouldSend()
    {
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
}

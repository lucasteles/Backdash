using Backdash.Core;
using Backdash.Network;

namespace Backdash.Tests.Utils;

static class Generate
{
    public static readonly Faker Faker = new();
    public static Randomizer Random => Faker.Random;

    public static Peer Peer() => Faker.Internet.IpEndPoint();

    public static ConnectionsState ConnectionsState() => new(Max.RemoteConnections);

    public static PlayerHandle PlayerHandle() => new(
        Random.Enum<PlayerType>(),
        Random.Int(1, Max.TotalPlayers)
    );

    public static GameInput GameInput(int frame, byte[] input)
    {
        if (input.Length > Max.CompressedBytes)
            throw new ArgumentOutOfRangeException(nameof(input));

        TestInput testInputBytes = new(input);
        GameInput result = new(testInputBytes)
        {
            Frame = new(frame),
        };
        return result;
    }

    public static GameInput[] GameInputRange(int count, int firstFrame = 0)
    {
        ThrowHelpers.ThrowIfArgumentIsNegative(count);
        return Generator().ToArray();

        IEnumerable<GameInput> Generator()
        {
            for (var i = 0; i < count; i++)
            {
                var bytes = Random.ArrayElement(GoodInputBytes);
                yield return GameInput(firstFrame + i, [bytes]);
            }
        }
    }

    public static readonly byte[] GoodInputBytes =
    [
        1 << 0,
        1 << 1,
        1 << 2,
        1 << 3,
        1 << 4,
        1 << 5,
        1 << 6,
        1 << 7,
    ];
}

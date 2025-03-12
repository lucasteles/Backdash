using System.Diagnostics.CodeAnalysis;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Comm;
using Backdash.Serialization;
using Backdash.Tests.Specs.Unit.Sync.Input;
using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Network.Protocol;

using static InputEncoderTests;

public class ProtocolInputBufferTests
{
    static ProtocolMessage GetSampleMessage(int startFrame = 0) => new(MessageType.Input)
    {
        Input = new()
        {
            InputSize = 1,
            Bits = new([3, 8, 224, 0, 5, 44, 32, 129, 7]),
            AckFrame = Frame.Null,
            StartFrame = new(startFrame),
            NumBits = 74,
            PeerConnectStatus = new(
                new ConnectionsState(4, Frame.Null).Statuses
            ),
            PeerCount = 4,
        },
    };

    static GameInput[] GetSampleInputs(int startFrame = 0) =>
    [
        Generate.GameInput(startFrame + 0, [1 << 0]),
        Generate.GameInput(startFrame + 1, [1 << 1]),
        Generate.GameInput(startFrame + 2, [1 << 2]),
        Generate.GameInput(startFrame + 3, [1 << 3]),
    ];

    [Fact]
    public void ValidateTestSampleSerialization()
    {
        var decompressedInput = DecompressToList(GetSampleMessage().Input);
        decompressedInput.Should().BeEquivalentTo(GetSampleInputs());
    }

    [DynamicFact(nameof(AutoFakeIt) + " requires dynamic code, even if the library is not annotated with " + nameof(RequiresDynamicCodeAttribute))]
    public void ShouldSendSingleInput()
    {
        var faker = GetFaker();
        var queue = faker.Generate<ProtocolInputBuffer<TestInput>>();
        var sender = faker.Resolve<IMessageSender>();
        var input = Generate.GameInput(0, [0, 0, 0, 2]);
        A.CallTo(() => faker.Resolve<IProtocolInbox<TestInput>>().LastAckedFrame).Returns(Frame.Null);
        ProtocolMessage message = new(MessageType.Input)
        {
            Input = new()
            {
                InputSize = 4,
                Bits = new([103]),
                AckFrame = Frame.Null,
                StartFrame = Frame.Zero,
                NumBits = 11,
                PeerConnectStatus = new(new ConnectionsState(4, Frame.Null).Statuses),
            },
        };
        A.CallTo(() => sender.SendMessage(in message)).Returns(true);
        var decompressedInput = DecompressToList(message.Input);
        decompressedInput.Single().Should().BeEquivalentTo(input);
        queue.SendInput(input).Should().Be(SendInputResult.Ok);
    }

    [DynamicFact(nameof(AutoFakeIt) + " requires dynamic code, even if the library is not annotated with " + nameof(RequiresDynamicCodeAttribute))]
    public void ShouldCompressMultipleBufferedInputs()
    {
        var faker = GetFakerWithSender();
        var queue = faker.Generate<ProtocolInputBuffer<TestInput>>();
        var sender = faker.Resolve<IMessageSender>();
        var inputs = GetSampleInputs();
        foreach (var input in inputs)
            queue.SendInput(input).Should().Be(SendInputResult.Ok);
        A.CallTo(sender)
            .Where(x => x.Method.Name == nameof(IMessageSender.SendMessage))
            .WithReturnType<bool>()
            .MustHaveHappened(inputs.Length, Times.Exactly);
        var lastMessageSent = Fake.GetCalls(sender).Last().Arguments.Single();
        lastMessageSent.Should().BeEquivalentTo(GetSampleMessage());
    }

    [DynamicFact(nameof(AutoFakeIt) + " requires dynamic code, even if the library is not annotated with " + nameof(RequiresDynamicCodeAttribute))]
    public void ShouldSkipAckedInputs()
    {
        var faker = GetFakerWithSender();
        var sender = faker.Resolve<IMessageSender>();
        var queue = faker.Generate<ProtocolInputBuffer<TestInput>>();
        // setting up first inputs
        GameInput[] previousInputs =
        [
            Generate.GameInput(0, [1 << 5]),
            Generate.GameInput(1, [0]),
        ];
        foreach (var input in previousInputs)
            queue.SendInput(input).Should().Be(SendInputResult.Ok);
        // setting up new inputs to check
        const int startFrame = 2;
        A.CallTo(() => faker.Resolve<IProtocolInbox<TestInput>>().LastAckedFrame).Returns(new(startFrame));
        var newInputs = GetSampleInputs(startFrame);
        foreach (var input in newInputs)
            queue.SendInput(input).Should().Be(SendInputResult.Ok);
        var expectedMessage = GetSampleMessage(startFrame);
        A.CallTo(sender)
            .Where(x => x.Method.Name == nameof(IMessageSender.SendMessage))
            .WithReturnType<bool>()
            .MustHaveHappened(previousInputs.Length + newInputs.Length, Times.Exactly);
        // assert only the last aggregated message
        var lastSentMessage = Fake.GetCalls(sender).Last().Arguments.Single();
        lastSentMessage.Should().BeEquivalentTo(expectedMessage);
    }

    [DynamicFact(nameof(AutoFakeIt) + " requires dynamic code, even if the library is not annotated with " + nameof(RequiresDynamicCodeAttribute))]
    public void ShouldHandleWhenMaxInputSizeReached()
    {
        var faker = GetFakerWithSender();
        var queue = faker.Generate<ProtocolInputBuffer<TestInput>>();
        var sender = faker.Resolve<IMessageSender>();
        var inputs = Generate.GameInputRange(Max.CompressedBytes * 4);
        List<GameInput> successfullySend = [];
        foreach (var input in inputs)
        {
            var op = queue.SendInput(input);
            if (op is SendInputResult.Ok)
                successfullySend.Add(input);
            else if (op is SendInputResult.MessageBodyOverflow)
                break;
            else
                Assert.Fail("Inputs setup");
        }

        var lastSend = queue.LastSent;
        var lastFrame = inputs.Max(i => i.Frame.Number);
        var calls = Fake.GetCalls(sender).ToArray();
        calls.Length.Should().Be(successfullySend.Count + 1);
        lastSend.Frame.Number.Should().BeLessThan(lastFrame);
        var message = calls
            .Where(x => x.Method.Name == nameof(IMessageSender.SendMessage))
            .Select(x => x.GetArgument<ProtocolMessage>(0))
            .Last();
        const int tolerance = ByteSize.ByteToBits;
        const int total = Max.CompressedBytes * ByteSize.ByteToBits;
        message.Input.NumBits.Should().BeInRange(total - tolerance, total + tolerance);
    }

    static AutoFakeIt GetFaker()
    {
        AutoFakeIt faker = new();
        ProtocolOptions options = new()
        {
            MaxPendingInputs = 1024,
        };
        ProtocolState state = new(
            Generate.PlayerHandle(),
            Generate.Peer(),
            Generate.ConnectionsState(),
            42
        )
        {
            CurrentStatus = ProtocolStatus.Running,
        };
        faker.Provide(Logger.CreateConsoleLogger(LogLevel.None));
        faker.Provide<IBinarySerializer<TestInput>>(new TestInputSerializer());
        faker.Provide(options);
        faker.Provide(state);
        return faker;
    }

    static AutoFakeIt GetFakerWithSender()
    {
        var faker = GetFaker();
        A.CallTo(() => faker.Resolve<IProtocolInbox<TestInput>>().LastAckedFrame).Returns(Frame.Null);
        A.CallTo(faker.Resolve<IMessageSender>())
            .Where(x => x.Method.Name == nameof(IMessageSender.SendMessage))
            .WithReturnType<bool>()
            .Returns(true);
        return faker;
    }
}

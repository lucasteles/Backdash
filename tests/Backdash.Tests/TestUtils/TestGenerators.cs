using System.Drawing;
using System.Numerics;
using System.Text;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Messages;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Tests.TestUtils.Types;
using FsCheck.Fluent;

namespace Backdash.Tests.TestUtils;

[Serializable, AttributeUsage(AttributeTargets.Method)]
public sealed class PropertyTestAttribute : FsCheck.Xunit.PropertyAttribute
{
    public PropertyTestAttribute()
    {
        QuietOnSuccess = true;
        MaxTest = 100;
        Arbitrary = [typeof(DataGenerator)];
    }
}

[Serializable]
abstract class DataGenerator
{
    static readonly Config config = Config.Default.WithArbitrary([typeof(DataGenerator)]);

    public static Gen<T> For<T>() => config.ArbMap.GeneratorFor<T>();

    public static T One<T>() => For<T>().Sample(1).Single();

    static Arbitrary<float> FloatGenerator() =>
        ArbMap.Default.GeneratorFor<float>()
            .Where(float.IsNormal)
            .ToArbitrary();

    static Arbitrary<double> DoubleGenerator() =>
        ArbMap.Default.GeneratorFor<double>()
            .Where(double.IsNormal)
            .ToArbitrary();

    static Arbitrary<Half> HalfGenerator() =>
        ArbMap.Default.GeneratorFor<float>()
            .Where(x => x >= (float)Half.MinValue && x < (float)Half.MaxValue)
            .Select(x => (Half)x)
            .ToArbitrary();

    static readonly IArbMap arb =
        ArbMap.Default
            .MergeArb(FloatGenerator())
            .MergeArb(DoubleGenerator())
            .MergeArb(HalfGenerator())
            .MergeArb(FrameGenerator());

    static Gen<T> Generate<T>() => arb.GeneratorFor<T>();

    public static Arbitrary<Frame> FrameGenerator() =>
        ArbMap.Default.GeneratorFor<PositiveInt>()
            .Select(x => new Frame(x.Item))
            .ToArbitrary();

    public static Arbitrary<TimeOnly> TimeOnlyGenerator() =>
        ArbMap.Default.GeneratorFor<long>()
            .Where(x => x >= TimeOnly.MinValue.Ticks && x <= TimeOnly.MaxValue.Ticks)
            .Select(x => new TimeOnly(x))
            .ToArbitrary();

    public static Arbitrary<DateOnly> DateOnlyGenerator() =>
        ArbMap.Default.GeneratorFor<DateTime>()
            .Select(x => new DateOnly(x.Year, x.Month, x.Day))
            .ToArbitrary();

    public static Arbitrary<Point> PointGenerator() => Arb.From(
        from x in Generate<int>()
        from y in Generate<int>()
        select new Point(x, y)
    );

    public static Arbitrary<StringBuilder> StringBuilderGenerator() => Arb.From(
        from s in Generate<string>()
        select new StringBuilder(s)
    );

    public static Arbitrary<CircularBuffer<T>> CircularBufferGenerator<T>(Arbitrary<T[]> valueArb) =>
        (
            from values in valueArb.Generator
            from size in Gen.Choose(values.Length, values.Length * 10)
            select (size, values)
        )
        .Select(v =>
        {
            var buffer = new CircularBuffer<T>(v.size);
            buffer.CopyFrom(v.values);
            return buffer;
        })
        .ToArbitrary();

    public static Arbitrary<SimpleStructData> SimpleStructDataGenerator(
        Arbitrary<Point> pointGenerator
    ) =>
        Arb.From(
            from f1 in Generate<int>()
            from f2 in Generate<uint>()
            from f3 in Generate<ulong>()
            from f4 in Generate<long>()
            from f5 in Generate<short>()
            from f6 in Generate<ushort>()
            from f7 in Generate<byte>()
            from f8 in Generate<sbyte>()
            from f9 in pointGenerator.Generator
            select new SimpleStructData
            {
                Field1 = f1,
                Field2 = f2,
                Field3 = f3,
                Field4 = f4,
                Field5 = f5,
                Field6 = f6,
                Field7 = f7,
                Field8 = f8,
                Field9 = f9,
            });

    public static Arbitrary<SimpleRefData> SimpleRefDataGenerator(
        Arbitrary<Point> pointGenerator
    ) =>
        Arb.From(
            from f1 in Generate<int>()
            from f2 in Generate<uint>()
            from f3 in Generate<ulong>()
            from f4 in Generate<long>()
            from f5 in Generate<short>()
            from f6 in Generate<ushort>()
            from f7 in Generate<byte>()
            from f8 in Generate<sbyte>()
            from f9 in pointGenerator.Generator
            select new SimpleRefData
            {
                Field1 = f1,
                Field2 = f2,
                Field3 = f3,
                Field4 = f4,
                Field5 = f5,
                Field6 = f6,
                Field7 = f7,
                Field8 = f8,
                Field9 = f9,
            });

    public static Arbitrary<ConnectStatus> ConnectStatusGenerator() => Arb.From(
        from disconnected in Generate<bool>()
        from lastFrame in Generate<Frame>()
        select new ConnectStatus
        {
            Disconnected = disconnected,
            LastFrame = lastFrame,
        }
    );

    public static Arbitrary<Header> HeaderGenerator() => Arb.From(
        from msgType in Generate<MessageType>()
        from magic in Generate<ushort>()
        from seqNum in Generate<ushort>()
        select new Header
        {
            Type = msgType,
            Magic = magic,
            SequenceNumber = seqNum,
        }
    );

    public static Arbitrary<InputAck> InputAckGenerator() => Arb.From(
        from frame in Generate<Frame>()
        select new InputAck
        {
            AckFrame = frame,
        }
    );

    public static Arbitrary<KeepAlive> KeepAliveGenerator() =>
        Gen.Constant(new KeepAlive()).ToArbitrary();

    public static Arbitrary<QualityReply> QualityReplyGenerator() => Arb.From(
        from pong in Generate<uint>()
        select new QualityReply
        {
            Pong = pong,
        }
    );

    public static Arbitrary<QualityReport> QualityReportGenerator() => Arb.From(
        from frameAdv in Generate<byte>()
        from ping in Generate<uint>()
        select new QualityReport
        {
            FrameAdvantage = frameAdv,
            Ping = ping,
        }
    );

    public static Arbitrary<SyncReply> SyncReplyGenerator() => Arb.From(
        from reply in Generate<uint>()
        from pong in Generate<uint>()
        select new SyncReply
        {
            RandomReply = reply,
            Pong = pong,
        }
    );

    public static Arbitrary<SyncRequest> SyncRequestGenerator() => Arb.From(
        from randRequest in Generate<uint>()
        select new SyncRequest
        {
            RandomRequest = randRequest,
            Ping = randRequest,
        }
    );

    public static Arbitrary<PeerStatusBuffer> PeerStatusBufferGenerator() =>
        Gen.Sized(testSize =>
            {
                var size = Math.Min(testSize, Max.NumberOfPlayers);
                return Generate<ConnectStatus>().ArrayOf(size);
            })
            .Select(arr =>
            {
                PeerStatusBuffer result = new();
                arr.CopyTo(result);
                return result;
            })
            .ToArbitrary();

    public static Arbitrary<ConsistencyCheckRequest> ConsistencyCheckRequestGenerator() => Arb.From(
        from frame in Generate<Frame>()
        select new ConsistencyCheckRequest
        {
            Frame = frame,
        }
    );

    public static Arbitrary<ConsistencyCheckReply> ConsistencyCheckReplyGenerator() => Arb.From(
        from frame in Generate<Frame>()
        from checksum in Generate<uint>()
        select new ConsistencyCheckReply
        {
            Frame = frame,
            Checksum = checksum,
        }
    );

    public static Arbitrary<InputMessage> InputMsgGenerator(
        Arbitrary<ConnectStatus> connectStatusGenerator
    ) =>
        Arb.From(
            from startFrame in Generate<Frame>()
            from disconnectReq in Generate<bool>()
            from ackFrame in Generate<Frame>()
            from peerCount in Gen.Choose(0, Max.NumberOfPlayers - 1)
            from peerConnectStats in connectStatusGenerator.Generator.ArrayOf(peerCount)
            from inputSize in Gen.Choose(sizeof(byte), sizeof(long))
            from inputBufferSize in Gen.Choose(0, Max.CompressedBytes - 1)
            from inputBuffer in Generate<byte>().ArrayOf(inputBufferSize)
            select new InputMessage
            {
                PeerCount = (byte)peerCount,
                PeerConnectStatus = new(peerConnectStats),
                StartFrame = startFrame,
                DisconnectRequested = disconnectReq,
                AckFrame = ackFrame,
                InputSize = (byte)inputSize,
                NumBits = checked((ushort)(inputBufferSize * ByteSize.ByteToBits)),
                Bits = new(inputBuffer),
            }
        );

    public static Arbitrary<ProtocolMessage> UpdMsgGenerator(
        Arbitrary<Header> headerArb,
        Arbitrary<SyncRequest> syncRequestArb,
        Arbitrary<SyncReply> syncReplyArb,
        Arbitrary<InputMessage> inputMessageArb,
        Arbitrary<QualityReport> qualityReportArb,
        Arbitrary<QualityReply> qualityReplyArb,
        Arbitrary<KeepAlive> keepAliveArb,
        Arbitrary<InputAck> inputAckArb,
        Arbitrary<ConsistencyCheckRequest> consistencyCheckReqArb,
        Arbitrary<ConsistencyCheckReply> consistencyCheckReplyArb
    ) =>
        headerArb.Generator
            .Where(h => h.Type is not MessageType.Unknown)
            .SelectMany(header => header.Type switch
            {
                MessageType.SyncRequest =>
                    syncRequestArb.Generator.Select(x => new ProtocolMessage
                    {
                        Header = header,
                        SyncRequest = x,
                    }),
                MessageType.SyncReply =>
                    syncReplyArb.Generator.Select(x => new ProtocolMessage
                    {
                        Header = header,
                        SyncReply = x,
                    }),
                MessageType.Input =>
                    inputMessageArb.Generator.Select(x => new ProtocolMessage
                    {
                        Header = header,
                        Input = x,
                    }),
                MessageType.QualityReport =>
                    qualityReportArb.Generator.Select(x => new ProtocolMessage
                    {
                        Header = header,
                        QualityReport = x,
                    }),
                MessageType.QualityReply =>
                    qualityReplyArb.Generator.Select(x => new ProtocolMessage
                    {
                        Header = header,
                        QualityReply = x,
                    }),
                MessageType.KeepAlive =>
                    keepAliveArb.Generator.Select(x => new ProtocolMessage
                    {
                        Header = header,
                        KeepAlive = x,
                    }),
                MessageType.ConsistencyCheckRequest =>
                    consistencyCheckReqArb.Generator.Select(x => new ProtocolMessage
                    {
                        Header = header,
                        ConsistencyCheckRequest = x,
                    }),
                MessageType.ConsistencyCheckReply =>
                    consistencyCheckReplyArb.Generator.Select(x => new ProtocolMessage
                    {
                        Header = header,
                        ConsistencyCheckReply = x,
                    }),
                MessageType.InputAck =>
                    inputAckArb.Generator.Select(x => new ProtocolMessage
                    {
                        Header = header,
                        InputAck = x,
                    }),
                _ => throw new ArgumentOutOfRangeException(nameof(header)),
            })
            .ToArbitrary();

    public static Arbitrary<TestInput> TestInputGenerator() => Arb.From(
        from bytes in Generate<byte>().ArrayOf(TestInput.Capacity)
        select new TestInput(bytes)
    );

    public static Arbitrary<Version> VersionGenerator() =>
        Generate<byte>()
            .Select(x => (int)x)
            .Four()
            .Select(values =>
            {
                var (ma, mi, bu, re) = values;
                return new Version(ma, mi, bu, re);
            })
            .ToArbitrary();

    public static Arbitrary<GameInput<TInput>> GameInputBufferGenerator<TInput>(
        Arbitrary<TInput> inputGenerator
    ) where TInput : unmanaged =>
        Arb.From(
            from frame in Generate<Frame>()
            from data in inputGenerator.Generator
            select new GameInput<TInput>()
            {
                Frame = frame,
                Data = data,
            }
        );

    public static Arbitrary<OddSizeArray<T>> OddSizeArrayGenerator<T>(
        Arbitrary<T[]> itemGenerator
    ) => itemGenerator.Generator
        .Where(x => x.Length % 2 is not 0)
        .Select(x => new OddSizeArray<T>(x))
        .ToArbitrary();

    public static Arbitrary<PendingGameInputs> PendingGameInputBufferGenerator(
        Arbitrary<GameInput> inputGenerator
    ) =>
        Gen.Sized(testSize =>
            {
                var size = Math.Clamp(testSize, 1, sizeof(int) * 2);
                var index = 1;
                var indexed = inputGenerator.Generator.Select(gi =>
                {
                    gi.Frame = new(index);
                    Interlocked.Increment(ref index);
                    return gi;
                });
                return indexed.ArrayOf(size);
            })
            .Select(gis => new PendingGameInputs(gis))
            .ToArbitrary();

    public static Arbitrary<Vector2> Vector2Generator() => Arb.From(
        from x in Generate<float>()
        from y in Generate<float>()
        select new Vector2(x, y)
    );

    public static Arbitrary<Vector3> Vector3Generator() => Arb.From(
        from x in Generate<float>()
        from y in Generate<float>()
        from z in Generate<float>()
        select new Vector3(x, y, z)
    );

    public static Arbitrary<Vector4> Vector4Generator() => Arb.From(
        from x in Generate<float>()
        from y in Generate<float>()
        from z in Generate<float>()
        from w in Generate<float>()
        select new Vector4(x, y, z, w)
    );

    public static Arbitrary<Quaternion> QuaternionGenerator() => Arb.From(
        from x in Generate<float>()
        from y in Generate<float>()
        from z in Generate<float>()
        from w in Generate<float>()
        select new Quaternion(x, y, z, w)
    );

    public static Arbitrary<ConfirmedInputs<T>> InputGroupGenerator<T>() where T : unmanaged =>
        Gen.Sized(testSize =>
            {
                var size = Math.Min(testSize, InputArray<T>.Capacity);
                return Generate<T>().ArrayOf(size);
            })
            .Select(arr => new ConfirmedInputs<T>(arr))
            .ToArbitrary();
}

record PendingGameInputs(GameInput[] Values);

public record OddSizeArray<T>(T[] Values)
{
    public override string ToString() => $"OddSizeArray[{string.Join(", ", Values)}]";
}

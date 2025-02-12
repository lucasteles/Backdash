using System.Drawing;
using System.Numerics;
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
        MaxTest = 200;
        Arbitrary = [typeof(PropertyTestGenerators)];
    }
}

static file class A
{
    public static Gen<T> Generate<T>() => ArbMap.Default.GeneratorFor<T>();
}

[Serializable]
class PropertyTestGenerators
{
    static Gen<T> G<T>() => ArbMap.Default.GeneratorFor<T>();

    public static Arbitrary<Point> PointGenerator() => Arb.From(
        from x in A.Generate<int>()
        from y in A.Generate<int>()
        select new Point(x, y)
    );

    public static Arbitrary<SimpleStructData> SimpleStructDataGenerator() =>
        Arb.From(
            from f1 in A.Generate<int>()
            from f2 in A.Generate<uint>()
            from f3 in A.Generate<ulong>()
            from f4 in A.Generate<long>()
            from f5 in A.Generate<short>()
            from f6 in A.Generate<ushort>()
            from f7 in A.Generate<byte>()
            from f8 in A.Generate<sbyte>()
            from f9 in A.Generate<Point>()
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

    public static Arbitrary<Frame> FrameGenerator() => Arb.From(
        from frame in A.Generate<PositiveInt>()
        select new Frame(frame.Item)
    );

    public static Arbitrary<ConnectStatus> ConnectStatusGenerator() => Arb.From(
        from disconnected in A.Generate<bool>()
        from lastFrame in A.Generate<Frame>()
        select new ConnectStatus
        {
            Disconnected = disconnected,
            LastFrame = lastFrame,
        }
    );

    public static Arbitrary<Header> HeaderGenerator() => Arb.From(
        from msgType in A.Generate<MessageType>()
        from magic in A.Generate<ushort>()
        from seqNum in A.Generate<ushort>()
        select new Header
        {
            Type = msgType,
            Magic = magic,
            SequenceNumber = seqNum,
        }
    );

    public static Arbitrary<float> FloatGenerator() =>
        ArbMap.Default.GeneratorFor<float>()
            .Where(float.IsNormal)
            .ToArbitrary();

    public static Arbitrary<double> DoubleGenerator() =>
        ArbMap.Default.GeneratorFor<double>()
            .Where(double.IsNormal)
            .ToArbitrary();

    public static Arbitrary<Half> HalfGenerator() =>
        ArbMap.Default.GeneratorFor<float>()
            .Where(x => x >= (float)Half.MinValue && x < (float)Half.MaxValue)
            .Select(x => (Half)x)
            .ToArbitrary();

    public static Arbitrary<InputAck> InputAckGenerator() => Arb.From(
        from frame in A.Generate<Frame>()
        select new InputAck
        {
            AckFrame = frame,
        }
    );

    public static Arbitrary<KeepAlive> KeepAliveGenerator() =>
        Gen.Constant(new KeepAlive()).ToArbitrary();

    public static Arbitrary<QualityReply> QualityReplyGenerator() => Arb.From(
        from pong in A.Generate<uint>()
        select new QualityReply
        {
            Pong = pong,
        }
    );

    public static Arbitrary<QualityReport> QualityReportGenerator() => Arb.From(
        from frameAdv in A.Generate<byte>()
        from ping in A.Generate<uint>()
        select new QualityReport
        {
            FrameAdvantage = frameAdv,
            Ping = ping,
        }
    );

    public static Arbitrary<SyncReply> SyncReplyGenerator() => Arb.From(
        from reply in A.Generate<uint>()
        from pong in A.Generate<uint>()
        select new SyncReply
        {
            RandomReply = reply,
            Pong = pong,
        }
    );

    public static Arbitrary<SyncRequest> SyncRequestGenerator() => Arb.From(
        from randRequest in A.Generate<uint>()
        select new SyncRequest
        {
            RandomRequest = randRequest,
            Ping = randRequest,
        }
    );

    public static Arbitrary<PeerStatusBuffer> PeerStatusBufferGenerator() =>
        Gen.Sized((testSize) =>
            {
                var size = Math.Min(testSize, Max.NumberOfPlayers);
                return A.Generate<ConnectStatus>().ArrayOf(size);
            })
            .Select(arr =>
            {
                PeerStatusBuffer result = new();
                arr.CopyTo(result);
                return result;
            })
            .ToArbitrary();

    public static Arbitrary<InputMessage> InputMsgGenerator() => Arb.From(
        from startFrame in A.Generate<Frame>()
        from disconnectReq in A.Generate<bool>()
        from ackFrame in A.Generate<Frame>()
        from inputSize in A.Generate<byte>()
        from peerConnectStats in Gen.Sized(testSize =>
        {
            var size = Math.Min(testSize, Max.NumberOfPlayers);
            return A.Generate<ConnectStatus>().ArrayOf(size);
        })
        from inputBuffer in Gen.Sized(testSize =>
        {
            var size = Math.Min(testSize, Max.CompressedBytes);
            return A.Generate<byte>().ArrayOf(size);
        })
        select new InputMessage
        {
            PeerConnectStatus = new(peerConnectStats),
            StartFrame = startFrame,
            DisconnectRequested = disconnectReq,
            AckFrame = ackFrame,
            InputSize = inputSize,
            NumBits = checked((ushort)(inputBuffer.Length * ByteSize.ByteToBits)),
            Bits = new(inputBuffer),
        }
    );

    public static Arbitrary<ProtocolMessage> UpdMsgGenerator() =>
        A.Generate<Header>()
            .Where(h => h.Type is not MessageType.Unknown)
            .SelectMany(header => header.Type switch
            {
                MessageType.SyncRequest =>
                    A.Generate<SyncRequest>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        SyncRequest = x,
                    }),
                MessageType.SyncReply =>
                    A.Generate<SyncReply>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        SyncReply = x,
                    }),
                MessageType.Input =>
                    A.Generate<InputMessage>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        Input = x,
                    }),
                MessageType.QualityReport =>
                    A.Generate<QualityReport>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        QualityReport = x,
                    }),
                MessageType.QualityReply =>
                    A.Generate<QualityReply>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        QualityReply = x,
                    }),
                MessageType.KeepAlive =>
                    A.Generate<KeepAlive>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        KeepAlive = x,
                    }),
                MessageType.InputAck =>
                    A.Generate<InputAck>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        InputAck = x,
                    }),
                _ => throw new ArgumentOutOfRangeException(nameof(header)),
            })
            .ToArbitrary();

    public static Arbitrary<TestInput> TestInputGenerator() => Arb.From(
        from bytes in A.Generate<byte>().ArrayOf(TestInput.Capacity)
        select new TestInput(bytes)
    );

    public static Arbitrary<Version> VersionGenerator() =>
        A.Generate<byte>()
            .Select(x => (int)x)
            .Four()
            .Select(values =>
            {
                var (ma, mi, bu, re) = values;
                return new Version(ma, mi, bu, re);
            })
            .ToArbitrary();

    public static Arbitrary<GameInput<TInput>> GameInputBufferGenerator<TInput>()
        where TInput : unmanaged
        => Arb.From(
            from frame in A.Generate<Frame>()
            from data in A.Generate<TInput>()
            select new GameInput<TInput>()
            {
                Frame = frame,
                Data = data,
            }
        );

    public static Arbitrary<PendingGameInputs> PendingGameInputBufferGenerator() =>
        Gen.Sized(testSize =>
            {
                var size = Math.Clamp(testSize, 1, sizeof(int) * 2);
                var index = 1;
                var indexed = A.Generate<GameInput>().Select(gi =>
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
        from x in A.Generate<float>()
        from y in A.Generate<float>()
        select new Vector2(x, y)
    );

    public static Arbitrary<Vector3> Vector3Generator() => Arb.From(
        from x in A.Generate<float>()
        from y in A.Generate<float>()
        from z in A.Generate<float>()
        select new Vector3(x, y, z)
    );

    public static Arbitrary<Vector4> Vector4Generator() => Arb.From(
        from x in A.Generate<float>()
        from y in A.Generate<float>()
        from z in A.Generate<float>()
        from w in A.Generate<float>()
        select new Vector4(x, y, z, w)
    );

    public static Arbitrary<Quaternion> QuaternionGenerator() => Arb.From(
        from x in A.Generate<float>()
        from y in A.Generate<float>()
        from z in A.Generate<float>()
        from w in A.Generate<float>()
        select new Quaternion(x, y, z, w)
    );

    public static Arbitrary<ConfirmedInputs<T>> InputGroupGenerator<T>() where T : unmanaged =>
        Gen.Sized(testSize =>
            {
                var size = Math.Min(testSize, InputArray<T>.Capacity);
                return A.Generate<T>().ArrayOf(size);
            })
            .Select(arr => new ConfirmedInputs<T>(arr))
            .ToArbitrary();
}

record PendingGameInputs(GameInput[] Values);

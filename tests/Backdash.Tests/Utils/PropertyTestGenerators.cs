using System.Drawing;
using System.Numerics;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Messages;
using Backdash.Sync;

namespace Backdash.Tests.Utils;

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

[Serializable]
class PropertyTestGenerators
{
    public static Arbitrary<Point> PointGenerator() => Arb.From(
        from x in Arb.Generate<int>()
        from y in Arb.Generate<int>()
        select new Point(x, y)
    );

    public static Arbitrary<SimpleStructData> SimpleStructDataGenerator() =>
        Arb.From(
            from f1 in Arb.Generate<int>()
            from f2 in Arb.Generate<uint>()
            from f3 in Arb.Generate<ulong>()
            from f4 in Arb.Generate<long>()
            from f5 in Arb.Generate<short>()
            from f6 in Arb.Generate<ushort>()
            from f7 in Arb.Generate<byte>()
            from f8 in Arb.Generate<sbyte>()
            from f9 in Arb.Generate<Point>()
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

    public static Arbitrary<MarshalStructData> MarshalStructDataGenerator() =>
        Arb.From(
            from f1 in Arb.Generate<int>()
            from f2 in Arb.Generate<long>()
            from f3 in Arb.Generate<byte>()
            from fArray in Arb.Generate<byte[]>().Where(a => a.Length is 10)
            select new MarshalStructData
            {
                Field1 = f1,
                Field2 = f2,
                Field3 = f3,
                FieldArray = fArray,
            });

    public static Arbitrary<Frame> FrameGenerator() => Arb.From(
        from frame in Arb.Generate<PositiveInt>()
        select new Frame(frame.Item)
    );

    public static Arbitrary<ConnectStatus> ConnectStatusGenerator() => Arb.From(
        from disconnected in Arb.Generate<bool>()
        from lastFrame in Arb.Generate<Frame>()
        select new ConnectStatus
        {
            Disconnected = disconnected,
            LastFrame = lastFrame,
        }
    );

    public static Arbitrary<Header> HeaderGenerator() => Arb.From(
        from msgType in Arb.Generate<MessageType>()
        from magic in Arb.Generate<ushort>()
        from seqNum in Arb.Generate<ushort>()
        select new Header
        {
            Type = msgType,
            Magic = magic,
            SequenceNumber = seqNum,
        }
    );

    public static Arbitrary<float> FloatGenerator() =>
        Arb.Default.Float32().Generator
            .Where(float.IsNormal)
            .ToArbitrary();

    public static Arbitrary<double> DoubleGenerator() =>
        Arb.Default.Float().Generator
            .Where(double.IsNormal)
            .ToArbitrary();

    public static Arbitrary<Half> HalfGenerator() =>
        Arb.Generate<float>()
            .Where(x => x >= (float)Half.MinValue && x < (float)Half.MaxValue)
            .Select(x => (Half)x)
            .ToArbitrary();

    public static Arbitrary<InputAck> InputAckGenerator() => Arb.From(
        from frame in Arb.Generate<Frame>()
        select new InputAck
        {
            AckFrame = frame,
        }
    );

    public static Arbitrary<KeepAlive> KeepAliveGenerator() =>
        Gen.Constant(new KeepAlive()).ToArbitrary();

    public static Arbitrary<QualityReply> QualityReplyGenerator() => Arb.From(
        from pong in Arb.Generate<uint>()
        select new QualityReply
        {
            Pong = pong,
        }
    );

    public static Arbitrary<QualityReport> QualityReportGenerator() => Arb.From(
        from frameAdv in Arb.Generate<byte>()
        from ping in Arb.Generate<uint>()
        select new QualityReport
        {
            FrameAdvantage = frameAdv,
            Ping = ping,
        }
    );

    public static Arbitrary<SyncReply> SyncReplyGenerator() => Arb.From(
        from reply in Arb.Generate<uint>()
        from pong in Arb.Generate<uint>()
        select new SyncReply
        {
            RandomReply = reply,
            Pong = pong,
        }
    );

    public static Arbitrary<SyncRequest> SyncRequestGenerator() => Arb.From(
        from randRequest in Arb.Generate<uint>()
        from remoteEp in Arb.Generate<byte>()
        select new SyncRequest
        {
            RandomRequest = randRequest,
            Ping = randRequest,
        }
    );

    public static Arbitrary<PeerStatusBuffer> PeerStatusBufferGenerator() =>
        Gen.Sized(testSize =>
            {
                var size = Math.Min(testSize, Max.RemoteConnections);
                return Gen.ArrayOf(size, Arb.Generate<ConnectStatus>());
            })
            .Select(arr =>
            {
                PeerStatusBuffer result = new();
                arr.CopyTo(result);
                return result;
            })
            .ToArbitrary();

    public static Arbitrary<InputMessage> InputMsgGenerator() => Arb.From(
        from startFrame in Arb.Generate<Frame>()
        from disconnectReq in Arb.Generate<bool>()
        from ackFrame in Arb.Generate<Frame>()
        from inputSize in Arb.Generate<byte>()
        from peerConnectStats in Gen.Sized(testSize =>
        {
            var size = Math.Min(testSize, Max.RemoteConnections);
            return Gen.ArrayOf(size, Arb.Generate<ConnectStatus>());
        })
        from inputBuffer in Gen.Sized(testSize =>
        {
            var size = Math.Min(testSize, Max.CompressedBytes);
            return Gen.ArrayOf(size, Arb.Generate<byte>());
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
        Arb.Generate<Header>()
            .Where(h => h.Type is not MessageType.Invalid)
            .SelectMany(header => header.Type switch
            {
                MessageType.SyncRequest =>
                    Arb.Generate<SyncRequest>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        SyncRequest = x,
                    }),
                MessageType.SyncReply =>
                    Arb.Generate<SyncReply>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        SyncReply = x,
                    }),
                MessageType.Input =>
                    Arb.Generate<InputMessage>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        Input = x,
                    }),
                MessageType.QualityReport =>
                    Arb.Generate<QualityReport>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        QualityReport = x,
                    }),
                MessageType.QualityReply =>
                    Arb.Generate<QualityReply>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        QualityReply = x,
                    }),
                MessageType.KeepAlive =>
                    Arb.Generate<KeepAlive>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        KeepAlive = x,
                    }),
                MessageType.InputAck =>
                    Arb.Generate<InputAck>().Select(x => new ProtocolMessage
                    {
                        Header = header,
                        InputAck = x,
                    }),
                _ => throw new ArgumentOutOfRangeException(nameof(header)),
            })
            .ToArbitrary();

    public static Arbitrary<TestInput> TestInputGenerator() => Arb.From(
        from bytes in Gen.ArrayOf(TestInput.Capacity, Arb.Generate<byte>())
        select new TestInput(bytes)
    );

    public static Arbitrary<Version> VersionGenerator() =>
        Arb.Generate<byte>()
            .Select(x => (int)x)
            .Four()
            .Select(values =>
            {
                var (ma, mi, bu, re) = values;
                return new Version(ma, mi, bu, re);
            })
            .ToArbitrary();

    public static Arbitrary<GameInput<TInput>> GameInputBufferGenerator<TInput>()
        where TInput : struct
        => Arb.From(
            from frame in Arb.Generate<Frame>()
            from data in Arb.Generate<TInput>()
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
                var indexed = Arb.Generate<GameInput>().Select(gi =>
                {
                    gi.Frame = new(index);
                    Interlocked.Increment(ref index);
                    return gi;
                });

                return Gen.ArrayOf(size, indexed);
            })
            .Select(gis => new PendingGameInputs(gis))
            .ToArbitrary();

    public static Arbitrary<Vector2> Vector2Generator() => Arb.From(
        from x in Arb.Generate<float>()
        from y in Arb.Generate<float>()
        select new Vector2(x, y)
    );

    public static Arbitrary<Vector3> Vector3Generator() => Arb.From(
        from x in Arb.Generate<float>()
        from y in Arb.Generate<float>()
        from z in Arb.Generate<float>()
        select new Vector3(x, y, z)
    );

    public static Arbitrary<Vector4> Vector4Generator() => Arb.From(
        from x in Arb.Generate<float>()
        from y in Arb.Generate<float>()
        from z in Arb.Generate<float>()
        from w in Arb.Generate<float>()
        select new Vector4(x, y, z, w)
    );

    public static Arbitrary<Quaternion> QuaternionGenerator() => Arb.From(
        from x in Arb.Generate<float>()
        from y in Arb.Generate<float>()
        from z in Arb.Generate<float>()
        from w in Arb.Generate<float>()
        select new Quaternion(x, y, z, w)
    );
}

record PendingGameInputs(GameInput[] Values);

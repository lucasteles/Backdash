using System.Drawing;
using nGGPO.Core;
using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Messages;

namespace nGGPO.Tests.Utils;

[Serializable, AttributeUsage(AttributeTargets.Method)]
public sealed class PropertyTestAttribute : FsCheck.Xunit.PropertyAttribute
{
    public PropertyTestAttribute()
    {
        QuietOnSuccess = true;
        MaxTest = 1000;
        Arbitrary = [typeof(MyGenerators)];
    }
}

[Serializable]
class MyGenerators
{
    public static Arbitrary<Point> PointGenerator() => Arb.From(
        from x in Arb.From<int>().Generator
        from y in Arb.From<int>().Generator
        select new Point(x, y)
    );

    public static Arbitrary<SimpleStructData> SimpleStructDataGenerator()
    {
        var generator =
            from f1 in Arb.From<int>().Generator
            from f2 in Arb.From<uint>().Generator
            from f3 in Arb.From<ulong>().Generator
            from f4 in Arb.From<long>().Generator
            from f5 in Arb.From<short>().Generator
            from f6 in Arb.From<ushort>().Generator
            from f7 in Arb.From<byte>().Generator
            from f8 in Arb.From<sbyte>().Generator
            from f9 in Arb.From<Point>().Generator
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
            };

        return Arb.From(generator);
    }

    public static Arbitrary<MarshalStructData> MarshalStructDataGenerator()
    {
        var generator =
            from f1 in Arb.From<int>().Generator
            from f2 in Arb.From<long>().Generator
            from f3 in Arb.From<byte>().Generator
            from fArray in Arb.From<byte[]>().Generator.Where(a => a.Length is 10)
            select new MarshalStructData
            {
                Field1 = f1,
                Field2 = f2,
                Field3 = f3,
                FieldArray = fArray,
            };

        return Arb.From(generator);
    }

    public static Arbitrary<Frame> FrameGenerator() => Arb.From(
        from frame in Arb.From<PositiveInt>().Generator
        select new Frame(frame.Item)
    );

    public static Arbitrary<ConnectStatus> ConnectStatusGenerator() => Arb.From(
        from disconnected in Arb.From<bool>().Generator
        from lastFrame in Arb.From<Frame>().Generator
        select new ConnectStatus
        {
            Disconnected = disconnected,
            LastFrame = lastFrame,
        }
    );

    public static Arbitrary<Header> HeaderGenerator() => Arb.From(
        from msgType in Arb.From<MsgType>().Generator
        from magic in Arb.From<ushort>().Generator
        from seqNum in Arb.From<ushort>().Generator
        select new Header
        {
            Type = msgType,
            Magic = magic,
            SequenceNumber = seqNum,
        }
    );

    public static Arbitrary<InputAck> InputAckGenerator() => Arb.From(
        from frame in Arb.From<int>().Generator
        select new InputAck
        {
            AckFrame = frame,
        }
    );

    public static Arbitrary<KeepAlive> KeepAliveGenerator() =>
        Arb.From(Gen.Constant(new KeepAlive()));

    public static Arbitrary<QualityReply> QualityReplyGenerator() => Arb.From(
        from pong in Arb.From<uint>().Generator
        select new QualityReply
        {
            Pong = pong,
        }
    );

    public static Arbitrary<QualityReport> QualityReportGenerator() => Arb.From(
        from frameAdv in Arb.From<byte>().Generator
        from ping in Arb.From<uint>().Generator
        select new QualityReport
        {
            FrameAdvantage = frameAdv,
            Ping = ping,
        }
    );

    public static Arbitrary<SyncReply> SyncReplyGenerator() => Arb.From(
        from reply in Arb.From<uint>().Generator
        select new SyncReply
        {
            RandomReply = reply,
        }
    );

    public static Arbitrary<SyncRequest> SyncRequestGenerator() => Arb.From(
        from randRequest in Arb.From<uint>().Generator
        from remoteMagic in Arb.From<ushort>().Generator
        from remoteEp in Arb.From<byte>().Generator
        select new SyncRequest
        {
            RandomRequest = randRequest,
            RemoteMagic = remoteMagic,
            RemoteEndpoint = remoteEp,
        }
    );

    public static Arbitrary<PeerStatusBuffer> PeerStatusBufferGenerator() =>
        Gen.Sized(testSize =>
            {
                var size = Math.Min(testSize, Max.MsgPlayers);
                return Gen.ArrayOf(size, Arb.From<ConnectStatus>().Generator);
            })
            .Select(arr =>
            {
                PeerStatusBuffer result = new();
                arr.CopyTo(result);
                return result;
            })
            .ToArbitrary();

    public static Arbitrary<InputMsg> InputMsgGenerator() => Arb.From(
        from startFrame in Arb.From<Frame>().Generator
        from disconnectReq in Arb.From<bool>().Generator
        from ackFrame in Arb.From<Frame>().Generator
        from numBits in Arb.From<ushort>().Generator
        from peerConnectStats in Gen.Sized(testSize =>
        {
            var size = Math.Min(testSize, Max.MsgPlayers);
            return Gen.ArrayOf(size, Arb.From<ConnectStatus>().Generator);
        })
        from inputBuffer in Gen.Sized(testSize =>
        {
            var size = Math.Min(testSize, Max.CompressedBytes);
            return Gen.ArrayOf(size, Arb.From<byte>().Generator);
        })
        select new InputMsg
        {
            PeerCount = (byte)peerConnectStats.Length,
            PeerConnectStatus = new(peerConnectStats),
            StartFrame = startFrame,
            DisconnectRequested = disconnectReq,
            AckFrame = ackFrame,
            NumBits = numBits,
            InputSize = (byte)inputBuffer.Length,
            Bits = new(inputBuffer),
        }
    );

    public static Arbitrary<ProtocolMessage> UpdMsgGenerator() => Arb.From<Header>().Generator
        .Where(h => h.Type is not MsgType.Invalid)
        .SelectMany(header => header.Type switch
        {
            MsgType.SyncRequest =>
                Arb.From<SyncRequest>().Generator.Select(x => new ProtocolMessage
                {
                    Header = header,
                    SyncRequest = x,
                }),
            MsgType.SyncReply =>
                Arb.From<SyncReply>().Generator.Select(x => new ProtocolMessage
                {
                    Header = header,
                    SyncReply = x,
                }),
            MsgType.Input =>
                Arb.From<InputMsg>().Generator.Select(x => new ProtocolMessage
                {
                    Header = header,
                    Input = x,
                }),
            MsgType.QualityReport =>
                Arb.From<QualityReport>().Generator.Select(x => new ProtocolMessage
                {
                    Header = header,
                    QualityReport = x,
                }),
            MsgType.QualityReply =>
                Arb.From<QualityReply>().Generator.Select(x => new ProtocolMessage
                {
                    Header = header,
                    QualityReply = x,
                }),
            MsgType.KeepAlive =>
                Arb.From<KeepAlive>().Generator.Select(x => new ProtocolMessage
                {
                    Header = header,
                    KeepAlive = x,
                }),
            MsgType.InputAck =>
                Arb.From<InputAck>().Generator.Select(x => new ProtocolMessage
                {
                    Header = header,
                    InputAck = x,
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(header)),
        })
        .ToArbitrary();

    public static Arbitrary<GameInput> GameInputBufferGenerator() => Arb.From(
        from frame in Arb.From<Frame>().Generator
        from bytes in Gen.ArrayOf(GameInputBuffer.Capacity / 2, Arb.From<byte>().Generator)
        // Gen.Sized(testSize => Gen.ArrayOf(Math.Min(testSize, GameInputBuffer.Capacity), Arb.From<byte>().Generator))
        select new GameInput(bytes)
        {
            Frame = frame,
        }
    );

    public static Arbitrary<PendingGameInputs> PendingGameInputBufferGenerator() =>
        Gen.Sized(testSize =>
            {
                var size = Math.Clamp(testSize, 1, Max.InputBytes);
                var index = 1;
                var indexed = Arb.From<GameInput>().Generator.Select(gi =>
                {
                    gi.Frame = new(index);
                    Interlocked.Increment(ref index);
                    return gi;
                });

                return Gen.ArrayOf(size, indexed);
            })
            .Select(gis => new PendingGameInputs(gis))
            .ToArbitrary();
}

record PendingGameInputs(GameInput[] Values);

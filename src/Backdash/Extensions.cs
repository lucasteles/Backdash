using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Protocol;
using Backdash.Serialization;

namespace Backdash;

static class InternalExtensions
{
    public static void EnqueueNext<T>(this Queue<T> queue, in T value)
    {
        var count = queue.Count;
        queue.Enqueue(value);
        for (var i = 0; i < count; i++)
            queue.Enqueue(queue.Dequeue());
    }

    public static int GetTypeSize<T>(this IBinarySerializer<T> serializer) where T : struct
    {
        var dummy = new T();
        Span<byte> buffer = stackalloc byte[Mem.MaxStackLimit];
        return serializer.Serialize(in dummy, buffer);
    }

    public static PlayerConnectionStatus ToPlayerStatus(this ProtocolStatus status) => status switch
    {
        ProtocolStatus.Syncing => PlayerConnectionStatus.Syncing,
        ProtocolStatus.Running => PlayerConnectionStatus.Connected,
        ProtocolStatus.Disconnected => PlayerConnectionStatus.Disconnected,
        _ => PlayerConnectionStatus.Unknown,
    };
}

using Backdash.Network;
using Backdash.Network.Protocol;

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

    public static PlayerConnectionStatus ToPlayerStatus(this ProtocolStatus status) => status switch
    {
        ProtocolStatus.Syncing => PlayerConnectionStatus.Syncing,
        ProtocolStatus.Running => PlayerConnectionStatus.Connected,
        ProtocolStatus.Disconnected => PlayerConnectionStatus.Disconnected,
        _ => PlayerConnectionStatus.Unknown,
    };

    public static IEnumerable<string> SplitToLines(this string value, int size)
    {
        var chunks = value.Chunk(size);
        foreach (var chars in chunks)
            yield return new(chars);
    }
}

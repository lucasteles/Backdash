using System.Runtime.CompilerServices;
using Backdash.Core;
using Backdash.Serialization;

namespace Backdash;

static class Extensions
{
    public static bool IsOk(this ResultCode code) => code is ResultCode.Ok;

    public static void AssertTrue(this bool value, [CallerMemberName] string? source = null)
    {
        if (!value)
            throw new BackdashException($"Unexpected behavior at {source}");
    }

    public static void EnqueueNext<T>(this Queue<T> queue, in T value)
    {
        var count = queue.Count;
        queue.Enqueue(value);
        for (var i = 0; i < count; i++)
            queue.Enqueue(queue.Dequeue());
    }

    public static int GetTypeSize<T>(this IBinaryWriter<T> serializer) where T : struct
    {
        var dummy = new T();
        Span<byte> buffer = stackalloc byte[Mem.MaxStackLimit];
        return serializer.Serialize(ref dummy, buffer);
    }
}

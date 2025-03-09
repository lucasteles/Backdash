using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Backdash.Synchronizing.Input.Confirmed;

class ConfirmedInputComparer<T>(EqualityComparer<T> inputComparer) : EqualityComparer<ConfirmedInputs<T>>
    where T : unmanaged
{
    public static EqualityComparer<ConfirmedInputs<T>> Create(EqualityComparer<T> inputComparer) =>
        inputComparer == EqualityComparer<T>.Default
            ? Default
            : new ConfirmedInputComparer<T>(inputComparer);

    public override bool Equals(ConfirmedInputs<T> x, ConfirmedInputs<T> y)
    {
        if (x.Count != y.Count) return false;
        var thisSpan = ((ReadOnlySpan<T>)x.Inputs)[..x.Count];
        var otherSpan = ((ReadOnlySpan<T>)y.Inputs)[..y.Count];
        return thisSpan.SequenceEqual(otherSpan, inputComparer);
    }

    public override int GetHashCode(ConfirmedInputs<T> obj)
    {
        HashCode hash = new();
        var span = (ReadOnlySpan<T>)obj.Inputs;
        ref var current = ref MemoryMarshal.GetReference(span);
        ref var limit = ref Unsafe.Add(ref current, obj.Count);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            hash.Add(current, inputComparer);
            current = ref Unsafe.Add(ref current, 1)!;
        }
        return hash.ToHashCode();
    }
}

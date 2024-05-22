using System.Buffers;
using System.Runtime.CompilerServices;
using Backdash.Core;

// ReSharper disable ParameterHidesMember
namespace Backdash.Data;

sealed unsafe class NativeMemoryManager<T> : MemoryManager<T>
    where T : unmanaged
{
    byte* pointer;
    int length;
    bool usingMemory;

    internal NativeMemoryManager(byte* pointer, int length)
    {
        this.pointer = pointer;
        this.length = length;
        usingMemory = false;
    }

    protected override void Dispose(bool disposing) { }

    public override Span<T> GetSpan()
    {
        usingMemory = true;
        return new(pointer, length);
    }

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        if ((uint)elementIndex >= (uint)length) ThrowHelpers.ThrowIndexOutOfRangeException();
        return new(pointer + (elementIndex * Unsafe.SizeOf<T>()), default, this);
    }

    public override void Unpin() { }

    public void EnableReuse() => usingMemory = false;

    public void Reset(byte* pointer, int length)
    {
        if (usingMemory) throw new InvalidOperationException("Memory is in use");
        this.pointer = pointer;
        this.length = length;
    }
}

using System.Collections;

namespace nGGPO.Data;

sealed class FrameArray<T>(int size) : IReadOnlyList<T>
{
    readonly T[] frames = new T[size];

    public IEnumerator<T> GetEnumerator() => (frames as IEnumerable<T>).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    int IReadOnlyCollection<T>.Count => frames.Length;
    T IReadOnlyList<T>.this[int index] => frames[index];

    public int Length => frames.Length;
    public ref T this[int index] => ref frames[index];
    public ref T this[in Frame index] => ref frames[index.Number];

    public void Fill(T value) => Array.Fill(frames, value);
}

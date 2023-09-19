﻿using System;
using System.Collections;
using System.Collections.Generic;
using nGGPO.Utils;

namespace nGGPO.DataStructure;

sealed class StaticBuffer<T> : IReadOnlyList<T> where T : notnull
{
    readonly T[] elements;

    public const int DefaultSize = 16;

    public StaticBuffer(int size) => elements = new T[size];

    public StaticBuffer() : this(DefaultSize)
    {
    }

    public int Size { get; private set; }

    int IReadOnlyCollection<T>.Count => Size;

    public void PushBack(T t)
    {
        Tracer.Assert(Size != elements.Length - 1);
        elements[Size++] = t;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Size; i++)
            yield return elements[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Clear()
    {
        Size = 0;
        Array.Clear(elements, 0, elements.Length);
    }

    public int IndexOf(T item)
    {
        for (var i = 0; i < Size; i++)
            if (elements[i].Equals(item))
                return i;

        return -1;
    }

    public bool Contains(T item) => IndexOf(item) >= 0;


    public ref T Ref(int index)
    {
        Tracer.Assert(index >= 0 && index < Size);
        return ref elements[index];
    }

    public T this[int index]
    {
        get
        {
            Tracer.Assert(index >= 0 && index < Size);
            return elements[index];
        }
        set
        {
            Tracer.Assert(index >= 0 && index < Size);
            elements[index] = value;
        }
    }
}
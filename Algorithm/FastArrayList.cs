using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Algorithm
{
    //Be careful! This is a mutable struct!
    //A simple implementation of generic list that exposes its content as reference.
    //Because we don't clear unused part of the array, the element type needs to be
    //unmanaged to avoid memory leak.
    public struct FastArrayList<T> where T : unmanaged
    {
        public T[] Data { get; private set; }
        public int Count { get; private set; }

        public void Clear()
        {
            Count = 0;
        }

        public void Add(T val)
        {
            EnsureSize();
            Data[Count++] = val;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (index != Count - 1)
            {
                Array.Copy(Data, index + 1, Data, index, Count - index - 1);
            }
            Count -= 1;
            Data[Count] = default;
        }

        public void Truncate(int newCount)
        {
            if (newCount < 0 || newCount > Count)
            {
                throw new ArgumentOutOfRangeException(nameof(newCount));
            }
            Count = newCount;
        }

        private void EnsureSize()
        {
            if (Data == null)
            {
                Data = new T[10];
                Count = 0;
            }
            if (Count == Data.Length)
            {
                var newData = new T[Data.Length * 2];
                Array.Copy(Data, newData, Count);
                Data = newData;
            }
        }

        public void EnsureSize(int totalSize)
        {
            if (Data.Length < totalSize)
            {
                var newData = new T[totalSize];
                Array.Copy(Data, newData, Count);
                Data = newData;
            }
        }

        public Span<T> ToSpan()
        {
            return new Span<T>(Data, 0, Count);
        }

        //Note that the list may reallocate while someone holding a reference.
        //So make sure no one is modifying the list if you hold a reference.
        public ref T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return ref Data[index];
            }
        }
    }
}

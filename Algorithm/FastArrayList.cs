using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PcrBattleChannel.Algorithm
{
    //Be careful! This is a mutable struct!
    //A simple implementation of generic list that exposes its content as reference.
    //Because we don't clear unused part of the array, the element type needs to be
    //unmanaged to avoid memory leak.
    public unsafe struct FastArrayList<T> : IDisposable where T : unmanaged
    {
        public const int AllocAlignment = 8 * 4; //AVX requires 32-byte (256-bit) alignment.

        private sealed class MemoryHolder : IDisposable
        {
            public IntPtr Mem { get; private set; }
            public int Size { get; private set; }
            private bool disposedValue;

            public MemoryHolder(int size)
            {
                Mem = Marshal.AllocHGlobal(size * sizeof(T) + AllocAlignment);
            }

            private void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    Marshal.FreeHGlobal(Mem);
                    Mem = IntPtr.Zero;
                    disposedValue = true;
                }
            }

            ~MemoryHolder()
            {
                Dispose(disposing: false);
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        private MemoryHolder _mem;
        private T* _data;
        public int Count { get; private set; }
        public int Capacity { get; private set; }

        public void Dispose()
        {
            _mem?.Dispose();
            Count = 0;
            _mem = null;
            _data = null;
            Capacity = 0;
        }

        public void Clear()
        {
            Count = 0;
        }

        public void Add(T val)
        {
            EnsureNextSize();
            _data[Count++] = val;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (index != Count - 1)
            {
                Buffer.MemoryCopy(&_data[index + 1], &_data[index], (Count - index) * sizeof(T), (Count - index - 1) * sizeof(T));
            }
            Count -= 1;
        }

        public void Truncate(int newCount)
        {
            if (newCount < 0 || newCount > Count)
            {
                throw new ArgumentOutOfRangeException(nameof(newCount));
            }
            Count = newCount;
        }

        private static unsafe (MemoryHolder mem, nint data) Allocate(int capacity)
        {
            var mem = new MemoryHolder(capacity);
            nint aligned = mem.Mem;
            aligned = (aligned + (AllocAlignment - 1)) & ~(AllocAlignment - 1);
            return (mem, aligned);
        }

        private void EnsureNextSize()
        {
            EnsureSizeInternal(Count + 1, Capacity == 0 ? 10 : Capacity * 2, updateCount: false);
        }

        public void EnsureSize(int size, bool updateCount)
        {
            EnsureSizeInternal(size, size, updateCount);
        }

        private void EnsureSizeInternal(int ensureSize, int allocateSize, bool updateCount)
        {
            Debug.Assert(ensureSize <= allocateSize);
            if (_data == null)
            {
                var (mem, data) = Allocate(allocateSize);
                _mem = mem;
                _data = (T*)data;
            }
            else if (Capacity < ensureSize)
            {
                var (newMem, newData) = Allocate(allocateSize);
                Buffer.MemoryCopy(_data, (T*)newData, allocateSize * sizeof(T), Count * sizeof(T));

                _mem?.Dispose();
                _mem = newMem;
                _data = (T*)newData;
                Capacity = allocateSize;
            }
            if (updateCount)
            {
                Count = ensureSize;
            }
        }

        //Note that the list may reallocate while someone holding a reference.
        //So make sure no one is modifying the list if you hold a reference.
        public Span<T> ToSpan()
        {
            return new Span<T>(_data, Count);
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
                return ref _data[index];
            }
        }

        //Note that the list may reallocate while someone holding a reference.
        //So make sure no one is modifying the list if you hold a reference.
        public ref T DataRef
        {
            get
            {
                if (_data == null)
                {
                    return ref Unsafe.NullRef<T>();
                }
                return ref _data[0];
            }
        }
    }
}

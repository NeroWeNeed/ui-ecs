using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace NeroWeNeed.UIECS
{
    public unsafe struct UIText
    {
        public long extraDataOffset;
    }

    public unsafe struct UTF8String : IDisposable, IEnumerable<uint>
    {
        internal const int PreambleSize = 3;
        public static readonly byte[] Preamble = new byte[] { 0xEF, 0XBB, 0XBF };
        internal const int LengthSize = sizeof(int);
        internal const int CharacterOffset = LengthSize + PreambleSize;
        internal byte* data;
        public int Capacity { get; private set; }
        internal int UsedCapacity { get; set; }
        public Allocator Allocator { get; }
        public bool IsCreated { get => Capacity > 0; }
        public int Length { get => IsCreated ? *(int*)data : 0; }
        public UTF8String(string str, Allocator allocator = Allocator.Temp)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            var length = str.Length;
            UsedCapacity = CharacterOffset + bytes.Length;
            Capacity = math.ceilpow2(UsedCapacity);
            data = (byte*)UnsafeUtility.Malloc(Capacity, 1, allocator);
            this.Allocator = allocator;
            UnsafeUtility.CopyStructureToPtr(ref length, data);
            fixed (byte* preambleLocation = Preamble)
            {
                UnsafeUtility.MemCpy(data + LengthSize, preambleLocation, PreambleSize);
            }
            fixed (byte* strLocation = bytes)
            {
                UnsafeUtility.MemCpy(data + CharacterOffset, strLocation, bytes.LongLength);
            }
        }
        public void SetValue(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            if (bytes.LongLength + CharacterOffset > Capacity)
            {
                Capacity = math.ceilpow2(CharacterOffset + bytes.Length);
                UsedCapacity = CharacterOffset + bytes.Length;
                UnsafeUtility.Free(data, Allocator);
                data = (byte*)UnsafeUtility.Malloc(Capacity, 1, Allocator);
            }
            fixed (byte* strLocation = bytes)
            {
                UnsafeUtility.MemCpy(data + CharacterOffset, strLocation, bytes.LongLength);
            }
        }

        public void Dispose()
        {
            Capacity = 0;
            UsedCapacity = 0;
            UnsafeUtility.Free(data, Allocator);
        }

        public IEnumerator<uint> GetEnumerator()
        {
            return new UTF8CodePointEnumerator(data);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new UTF8CodePointEnumerator(data);
        }
        public override string ToString()
        {
            return Encoding.UTF8.GetString(data + CharacterOffset, UsedCapacity - CharacterOffset);
        }
    }
    public unsafe struct UTF8CodePointEnumerator : IEnumerator<uint>
    {
        
        private uint current;
        private readonly byte* location;
        private readonly ulong handle;
        public int offset;
        public uint Current => current;

        object System.Collections.IEnumerator.Current => current;
        public UTF8CodePointEnumerator(byte* utf8String, bool hasSize = true, bool hasPreamble = true)
        {
            location = utf8String;
            if (hasSize)
            {
                location += UTF8String.LengthSize;
            }
            if (hasPreamble)
            {
                location += UTF8String.PreambleSize;
            }
            current = 0;
            offset = 0;
            handle = 0;
        }
        public UTF8CodePointEnumerator(string str)
        {
            location = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(Encoding.UTF8.GetBytes(str), out ulong gcHandle);
            handle = gcHandle;
            current = 0;
            offset = 0;
        }

        public void Dispose()
        {
            if (handle != 0)
            {
                UnsafeUtility.ReleaseGCObject(handle);
            }
        }

        public bool MoveNext()
        {
            do
            {
                byte buffer = location[offset];
                if (buffer <= 0x7F)
                {
                    current = buffer;
                    offset++;
                    break;
                }
                else if (buffer <= 0xBF)
                {
                    current <<= 6;
                    current |= (uint)(buffer & 0x3F);
                    offset++;
                }
                else if (buffer <= 0xDF)
                {
                    current = (uint)(buffer & 0x1F);
                    offset++;
                }
                else if (buffer <= 0xEF)
                {
                    current = (uint)(buffer & 0xF);
                    offset++;
                }
                else if (buffer <= 0xF7)
                {
                    current = (uint)(buffer & 0x7);
                    offset++;
                }
                else
                {
                    return false;
                }
            } while (location[offset] >= 0x80 && location[offset] <= 0xBF);

            return current != 0;
        }

        public void Reset()
        {
            offset = 0;
            current = 0;
        }
    }
}
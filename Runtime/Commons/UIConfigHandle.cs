using System.Collections.Generic;
using System.Collections;
using Unity.Mathematics;
using System;
using Unity.Collections;
using System.Text;

namespace NeroWeNeed.UIECS
{
    public unsafe struct UIExtraDataHandle
    {
        public void* value;

        public UIExtraDataHandle(void* value)
        {
            this.value = value;
        }
        public UIExtraDataExtendedHandle ToExtended(long length) => new UIExtraDataExtendedHandle(value, length);
        public sbyte SByte => *(sbyte*)value;
        public short Short => *(short*)value;
        public int Int => *(int*)value;
        public long Long => *(long*)value;
        public byte Byte => *(byte*)value;
        public ushort UShort => *(ushort*)value;
        public uint UInt => *(uint*)value;
        public ulong ULong => *(ulong*)value;
        public float Float => *(float*)value;
        public double Double => *(double*)value;
        
    }
    public unsafe struct UIExtraDataExtendedHandle
    {
        public void* value;
        public long length;
        public UIExtraDataExtendedHandle(void* value, long length)
        {
            this.value = value;
            this.length = length;
        }
        public static implicit operator UIExtraDataHandle(UIExtraDataExtendedHandle extendedHandle) => new UIExtraDataHandle(extendedHandle.value);


    }

    /// <summary>
    /// Handle for UI structures stored in the following format:
    /// [UIRuntimeDataHandle][UIModelConfigHeader][UIPropertyBlocks][ExtraData]
    /// </summary>
    public unsafe struct UIConfigHandle : IEnumerable<UIConfigHandleEnumerator.Item>
    {
        public void* value;
        public int PropertyBlockCount { get => ((UIModelConfigHeader*)(((byte*)value) + sizeof(UIRuntimeDataHeader)))->count; }
        public UIConfigHandleEnumerator GetEnumerator()
        {
            return new UIConfigHandleEnumerator(ref this);
        }

        IEnumerator<UIConfigHandleEnumerator.Item> IEnumerable<UIConfigHandleEnumerator.Item>.GetEnumerator()
        {
            return new UIConfigHandleEnumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new UIConfigHandleEnumerator(ref this);
        }
    }
    public unsafe struct UIConfigHandleEnumerator : IEnumerator<UIConfigHandleEnumerator.Item>
    {

        private byte* data;
        private int blockCount;
        private int offset;
        private int index;
        private Item current;

        public Item Current => current;

        object System.Collections.IEnumerator.Current => current;

        public UIConfigHandleEnumerator(ref UIConfigHandle data)
        {
            this.data = (byte*)data.value;
            blockCount = data.PropertyBlockCount;
            index = -1;
            offset = 0;
            current = default;
        }

        public bool MoveNext()
        {
            offset += index < 0 ? sizeof(UIRuntimeDataHeader) + sizeof(UIModelConfigHeader) : (((UIModelConfigPropertyBlockHeader*)(data + offset))->length + sizeof(UIModelConfigPropertyBlockHeader));
            index++;
            current.header = (UIModelConfigPropertyBlockHeader*)(data + offset);
            current.data = data + offset + sizeof(UIModelConfigPropertyBlockHeader);

            return index < blockCount;

        }

        public void Reset()
        {
            index = -1;
            offset = 0;
            current = default;
        }

        public void Dispose()
        {

        }
        public struct Item
        {
            public UIModelConfigPropertyBlockHeader* header;
            public void* data;

        }

    }
    public struct UIRuntimeDataHeader
    {
        public float4 constraints;
        public float2 size;
        public float2 position;
    }
    public struct UIModelConfigHeader
    {
        /// <summary>
        /// Represents the entire length of the config for the node. Does not the size of the header.
        /// </summary>
        public int length;
        public ulong element;
        /// <summary>
        /// Represents how many config blocks are present
        /// </summary>
        public int count;
    }
    public unsafe struct UIModelConfigPropertyBlockHeader
    {
        /// <summary>
        /// Represents the entire length of the config block denoted by id. Does not include the hash, length, or enabled fields.
        /// </summary>

        public int length;

        public ulong hash;
        public byte enabled;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NeroWeNeed.UIECS
{
    [BurstCompile]
    public unsafe static class UIExtraDataHandleExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIExtraDataHandle GetHandle(this DynamicBuffer<UIConfigBufferExtraData> self) => new UIExtraDataHandle { value = self.GetUnsafePtr() };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIExtraDataHandle GetReadOnlyHandle(this DynamicBuffer<UIConfigBufferExtraData> self) => new UIExtraDataHandle { value = self.GetUnsafeReadOnlyPtr() };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIExtraDataExtendedHandle GetExtendedHandle(this DynamicBuffer<UIConfigBufferExtraData> self) => new UIExtraDataExtendedHandle(self.GetUnsafePtr(), self.Length);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIExtraDataExtendedHandle GetExtendedReadOnlyHandle(this DynamicBuffer<UIConfigBufferExtraData> self) => new UIExtraDataExtendedHandle(self.GetUnsafeReadOnlyPtr(), self.Length);
    }

    [BurstCompile]
    public unsafe static class UIExtraDataHandleTextExtensions
    {
        private const int PreambleOffset = sizeof(int);
        private const byte ContinuedByteData = 0b00111111;
        [BurstCompile]
        public static UnicodeEncodingType GetEncoding(this UIExtraDataHandle self) => GetEncoding((byte*)self.value + PreambleOffset);
        [BurstCompile]
        public static UnicodeEncodingType GetEncoding(byte* bytes)
        {

            if (bytes[0] == 0xFE && bytes[1] == 0xFF)
                return UnicodeEncodingType.UTF16BE;
            if (bytes[0] == 0xFF && bytes[1] == 0xFE)
                return UnicodeEncodingType.UTF16LE;
            if (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return UnicodeEncodingType.UTF8;
            if (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
                return UnicodeEncodingType.UTF32BE;
            if (bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
                return UnicodeEncodingType.UTF32LE;
            return UnicodeEncodingType.Unknown;
        }
        [BurstCompile]
        public static int GetPreambleSize(UnicodeEncodingType encodingType)
        {
            switch (encodingType)
            {
                case UnicodeEncodingType.UTF8:
                    return 3;
                case UnicodeEncodingType.UTF16BE:
                case UnicodeEncodingType.UTF16LE:
                    return 2;
                case UnicodeEncodingType.UTF32BE:
                case UnicodeEncodingType.UTF32LE:
                    return 4;
                default:
                    return 0;

            }
        }
        [BurstCompile]
        public static int GetTextLength(this UIExtraDataHandle self)
        {
            return *(int*)self.value;
        }
/*         [BurstCompile]
        public static long GetTextByteCount(this UIExtraDataExtendedHandle self)
        {
            return self.length - sizeof(int) - GetPreambleSize(self);
        }
        [BurstCompile]
        public static long GetTextByteCount(this UIExtraDataExtendedHandle self, UnicodeEncodingType encodingType)
        {
            return self.length - sizeof(int) - GetPreambleSize(encodingType);
        } */
        [BurstCompile]
        public static byte* GetTextContent(this UIExtraDataHandle self, UnicodeEncodingType encodingType)
        {
            return ((byte*)self.value) + sizeof(int) + GetPreambleSize(encodingType);
        }
        [BurstCompile]
        public static byte* GetTextContent(this UIExtraDataHandle self)
        {
            return ((byte*)self.value) + sizeof(int) + self.GetPreambleSize();
        }
        [BurstCompile]
        public static int GetPreambleSize(this UIExtraDataHandle self) => GetPreambleSize(self.GetEncoding());
        [BurstCompile]
        public static int GetMinCharSize(UnicodeEncodingType encodingType)
        {
            switch (encodingType)
            {
                case UnicodeEncodingType.UTF8:
                    return 1;
                case UnicodeEncodingType.UTF16BE:
                case UnicodeEncodingType.UTF16LE:
                    return 2;
                case UnicodeEncodingType.UTF32BE:
                case UnicodeEncodingType.UTF32LE:
                    return 4;
                default:
                    return 0;
            }
        }
        [BurstCompile]
        public static int GetMaxCharSize(UnicodeEncodingType encodingType)
        {
            switch (encodingType)
            {
                case UnicodeEncodingType.UTF8:
                case UnicodeEncodingType.UTF16BE:
                case UnicodeEncodingType.UTF16LE:
                case UnicodeEncodingType.UTF32BE:
                case UnicodeEncodingType.UTF32LE:
                    return 4;
                default:
                    return 0;
            }
        }
        public static UTF8CodePointEnumerator GetEnumerator(this UIExtraDataHandle handle) => new UTF8CodePointEnumerator((byte*)handle.value);


    }
    public enum UnicodeEncodingType : byte
    {
        None, UTF8, UTF16BE, UTF16LE, UTF32BE, UTF32LE, Unknown
    }
    [BurstCompile]
    public unsafe static class UIConfigHandleExtensions
    {


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIConfigHandle GetHandle(this DynamicBuffer<UIConfigBufferData> self) => new UIConfigHandle { value = self.GetUnsafePtr() };



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIConfigHandle GetReadOnlyHandle(this DynamicBuffer<UIConfigBufferData> self) => new UIConfigHandle { value = self.GetUnsafeReadOnlyPtr() };
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetPropertyBlock<TBlock>(this UIConfigHandle self, out TBlock* propertyBlock, bool checkEnabled = true) where TBlock : unmanaged, IPropertyBlock
        {
            var targetHash = UIElementManager.GetPropertyBlock<TBlock>();
            var totalBlocks = self.PropertyBlockCount;
            propertyBlock = (TBlock*)IntPtr.Zero;
            //UIModelConfigPropertyBlockHeader* start = (UIModelConfigPropertyBlockHeader*)IntPtr.Add((IntPtr)self.value, UnsafeUtility.SizeOf<UIRuntimeDataHeader>() + UnsafeUtility.SizeOf<UIModelConfigHeader>()).ToPointer();
            UIModelConfigPropertyBlockHeader* start = (UIModelConfigPropertyBlockHeader*)(((byte*)self.value) + UnsafeUtility.SizeOf<UIRuntimeDataHeader>() + UnsafeUtility.SizeOf<UIModelConfigHeader>());
            for (int i = 0; i < totalBlocks && start->hash <= targetHash; i++)
            {
                if (start->hash == targetHash && ((checkEnabled && start->enabled != 0) || !checkEnabled))
                {
                    propertyBlock = (TBlock*)(((byte*)start) + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>());
                    return true;
                }
                start = (UIModelConfigPropertyBlockHeader*)(((byte*)start) + start->length + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>());
            }
            return false;
        }

        public static TBlock* GetPropertyFromOffset<TBlock>(this UIConfigHandle self, int offset, bool checkEnabled = true) where TBlock : unmanaged, IPropertyBlock
        {
            var targetHash = UIElementManager.GetPropertyBlock<TBlock>();
            var totalBlocks = self.PropertyBlockCount;
            UIModelConfigPropertyBlockHeader* start = (UIModelConfigPropertyBlockHeader*)(((byte*)self.value) + UnsafeUtility.SizeOf<UIRuntimeDataHeader>() + UnsafeUtility.SizeOf<UIModelConfigHeader>());
            for (int i = 0; i < totalBlocks && start->hash <= targetHash; i++)
            {
                if (start->hash == targetHash && ((checkEnabled && start->enabled != 0) || !checkEnabled))
                {
                    return (TBlock*)(((byte*)start) + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>() + offset);
                }
                start = (UIModelConfigPropertyBlockHeader*)(((byte*)start) + start->length + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>());
            }
            return (TBlock*)IntPtr.Zero;
        }
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TBlock* GetPropertyBlock<TBlock>(this UIConfigHandle self) where TBlock : unmanaged, IPropertyBlock
        {
            var targetHash = UIElementManager.GetPropertyBlock<TBlock>();
            var totalBlocks = self.PropertyBlockCount;
            UIModelConfigPropertyBlockHeader* start = (UIModelConfigPropertyBlockHeader*)(((byte*)self.value) + UnsafeUtility.SizeOf<UIRuntimeDataHeader>() + UnsafeUtility.SizeOf<UIModelConfigHeader>());
            for (int i = 0; i < totalBlocks && start->hash <= targetHash; i++)
            {
                if (start->hash == targetHash)
                {
                    return (TBlock*)(((byte*)start) + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>());
                }
                start = (UIModelConfigPropertyBlockHeader*)(((byte*)start) + start->length + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>());
            }
            return (TBlock*)IntPtr.Zero;
        }
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* GetPropertyBlockFromHash(this UIConfigHandle self, ulong targetHash)
        {

            var totalBlocks = self.PropertyBlockCount;
            UIModelConfigPropertyBlockHeader* start = (UIModelConfigPropertyBlockHeader*)(((byte*)self.value) + UnsafeUtility.SizeOf<UIRuntimeDataHeader>() + UnsafeUtility.SizeOf<UIModelConfigHeader>());
            for (int i = 0; i < totalBlocks && start->hash <= targetHash; i++)
            {
                if (start->hash == targetHash)
                {
                    return ((byte*)start) + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>();
                }
                start = (UIModelConfigPropertyBlockHeader*)(((byte*)start) + start->length + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>());
            }
            return (void*)IntPtr.Zero;
        }
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIModelConfigPropertyBlockHeader* GetPropertyBlockHeader<TBlock>(this UIConfigHandle self) where TBlock : unmanaged, IPropertyBlock
        {
            var targetHash = UIElementManager.GetPropertyBlock<TBlock>();
            var totalBlocks = self.PropertyBlockCount;
            UIModelConfigPropertyBlockHeader* start = (UIModelConfigPropertyBlockHeader*)(((byte*)self.value) + UnsafeUtility.SizeOf<UIRuntimeDataHeader>() + UnsafeUtility.SizeOf<UIModelConfigHeader>());
            for (int i = 0; i < totalBlocks && start->hash <= targetHash; i++)
            {
                if (start->hash == targetHash)
                {
                    return start;
                }
                start = (UIModelConfigPropertyBlockHeader*)(((byte*)start) + start->length + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>());
            }
            return (UIModelConfigPropertyBlockHeader*)IntPtr.Zero;
        }
        public static bool TryGetProperty<TProperty>(this UIConfigHandle self, UIProperty property, out TProperty* location) where TProperty : unmanaged
        {
            var totalBlocks = self.PropertyBlockCount;
            UIModelConfigPropertyBlockHeader* start = (UIModelConfigPropertyBlockHeader*)(((byte*)self.value) + UnsafeUtility.SizeOf<UIRuntimeDataHeader>() + UnsafeUtility.SizeOf<UIModelConfigHeader>());
            for (int i = 0; i < totalBlocks && start->hash <= property.blockHash; i++)
            {
                if (start->hash == property.blockHash)
                {
                    location = (TProperty*)((byte*)start + (UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>() + property.offset));
                    return true;
                }
                start = (UIModelConfigPropertyBlockHeader*)(((byte*)start) + start->length + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>());
            }
            location = (TProperty*)IntPtr.Zero;
            return false;

        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint TotalLength(this UIConfigHandle self) => (uint)(self.Length() + UnsafeUtility.SizeOf<UIRuntimeDataHeader>() + UnsafeUtility.SizeOf<UIModelConfigHeader>());
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Length(this UIConfigHandle self) => ((UIModelConfigHeader*)(((byte*)self.value) + UnsafeUtility.SizeOf<UIRuntimeDataHeader>()))->length;

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Element(this UIConfigHandle self) => ((UIModelConfigHeader*)(((byte*)self.value) + UnsafeUtility.SizeOf<UIRuntimeDataHeader>()))->element;


        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetConstraints(this UIConfigHandle self, in float4 value)
        {
            var selfPtr = (UIRuntimeDataHeader*)self.value;
            selfPtr->constraints = value;
        }
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetConstraints(this UIConfigHandle self, out float4 value)
        {
            var selfPtr = (UIRuntimeDataHeader*)self.value;
            value = selfPtr->constraints;
        }
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetSize(this UIConfigHandle self, in float2 value)
        {
            var selfPtr = (UIRuntimeDataHeader*)self.value;
            selfPtr->size = value;
        }
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetSize(this UIConfigHandle self, out float2 value)
        {
            var selfPtr = (UIRuntimeDataHeader*)self.value;
            value = selfPtr->size;
        }
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetWidth(this UIConfigHandle self)
        {
            var selfPtr = (UIRuntimeDataHeader*)self.value;
            return selfPtr->size.x;
        }
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetHeight(this UIConfigHandle self)
        {
            var selfPtr = (UIRuntimeDataHeader*)self.value;
            return selfPtr->size.y;
        }
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPosition(this UIConfigHandle self, in float2 value)
        {
            var selfPtr = (UIRuntimeDataHeader*)self.value;
            selfPtr->position = value;
        }
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetPosition(this UIConfigHandle self, out float2 value)
        {
            var selfPtr = (UIRuntimeDataHeader*)self.value;
            value = selfPtr->position;
        }
    }
}
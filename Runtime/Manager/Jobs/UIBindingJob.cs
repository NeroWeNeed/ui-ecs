using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


namespace NeroWeNeed.UIECS.Jobs
{
    [BurstCompile]
    public static class UIBindingJobUtility
    {
        
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetComponentTypeHandle<TComponent, TUIProperty>(this ref UIBindingJob<TComponent, TUIProperty> job, ComponentTypeHandle<TComponent> handle) where TComponent : unmanaged, IUIMaterialProperty, IComponentData where TUIProperty : unmanaged
        {
            job.componentTypeHandle = handle;

        }
    }
    public unsafe struct BindingHintData
    {
        public const int HintSize = 4;
        public fixed byte value[4];
        public static BindingHintData Create(NormalizerHint x = NormalizerHint.None, NormalizerHint y = NormalizerHint.None, NormalizerHint z = NormalizerHint.None, NormalizerHint w = NormalizerHint.None)
        {
            return new BindingHintData(x, y, z, w);
        }
        public BindingHintData(NormalizerHint x, NormalizerHint y, NormalizerHint z, NormalizerHint w)
        {
            fixed (byte* hints = this.value)
            {
                hints[0] = (byte)x;
                hints[1] = (byte)y;
                hints[2] = (byte)z;
                hints[3] = (byte)w;
            }
        }
        public NormalizerHint this[int index] {
            get => (NormalizerHint)value[index];
        }
    }
    [BurstCompile]
    public unsafe struct UIBindingJob<TComponent, TUIProperty> : IJobEntityBatch where TComponent : unmanaged, IUIMaterialProperty, IComponentData where TUIProperty : unmanaged
    {
        [ReadOnly]
        public BufferTypeHandle<UIConfigBufferData> configBufferTypeHandle;
        public ComponentTypeHandle<TComponent> componentTypeHandle;
        public UIProperty property;
        public TypeCode sourceTypeCode;
        public int fieldOffset;
        public int vectorLength;
        public BindingHintData hints;
        public UIBindingJob(
            BufferTypeHandle<UIConfigBufferData> configBufferTypeHandle,
            ComponentTypeHandle<TComponent> componentTypeHandle,
            UIProperty property,
            TypeCode sourceTypeCode,
            int fieldOffset,
            int vectorLength,
            BindingHintData hints) : this()
        {
            this.configBufferTypeHandle = configBufferTypeHandle;
            this.componentTypeHandle = componentTypeHandle;

            this.property = property;
            this.sourceTypeCode = sourceTypeCode;
            this.fieldOffset = fieldOffset;
            this.vectorLength = vectorLength;
            this.hints = hints;
        }

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var buffers = batchInChunk.GetBufferAccessor(configBufferTypeHandle);
            var components = batchInChunk.GetNativeArray(componentTypeHandle);
            var componentPtr = components.GetUnsafePtr();
            var stride = UnsafeUtility.SizeOf<TUIProperty>() - fieldOffset - GetSize(sourceTypeCode);

            for (int index = 0; index < batchInChunk.Count; index++)
            {
                var handle = buffers[index].GetReadOnlyHandle();
                var normalizer = BuildNormalizer(handle);
                if (handle.TryGetProperty(property, out TUIProperty* location))
                {
                    byte* ptr = (byte*)location;
                    int pos = 0;
                    for (int i = 0; i < vectorLength; i++)
                    {
                        pos += fieldOffset;
                        UnsafeUtility.WriteArrayElement<float>(componentPtr, (index * vectorLength) + i, ToFloat(sourceTypeCode, ptr + fieldOffset) / GetNormalizerForIndex(normalizer, i));
                        ptr += stride;
                    }
                }
                else
                {
                    for (int i = 0; i < vectorLength; i++)
                    {
                        UnsafeUtility.WriteArrayElement<float>(componentPtr, (index * vectorLength) + i, default);
                    }
                }
            }
        }
        private float4 BuildNormalizer(UIConfigHandle handle)
        {
            return new float4(
                GetNormalizerValue(hints[0], handle),
                GetNormalizerValue(hints[1], handle),
                GetNormalizerValue(hints[2], handle),
                GetNormalizerValue(hints[3], handle)
                );
        }
        private float GetNormalizerValue(NormalizerHint hint, UIConfigHandle handle)
        {
            switch (hint)
            {
                case NormalizerHint.Color:
                    return 255f;
                case NormalizerHint.Width:
                    var w = handle.GetWidth();
                    return w > 0 ? w : 1f;
                case NormalizerHint.Height:
                    var h = handle.GetHeight();
                    return h > 0 ? h : 1f;
                default:
                    return 1f;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetNormalizerForIndex(float4 normalizer, int i)
        {
            switch (i)
            {
                case 1:
                    return normalizer.x;
                case 2:
                    return normalizer.y;
                case 3:
                    return normalizer.z;
                case 4:
                    return normalizer.w;
                default:
                    return 1;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetSize(TypeCode sourceTypeCode)
        {
            switch (sourceTypeCode)
            {
                case TypeCode.Byte:
                    return sizeof(byte);
                case TypeCode.Double:
                    return sizeof(double);
                case TypeCode.Int16:
                    return sizeof(short);
                case TypeCode.Int32:
                    return sizeof(int);
                case TypeCode.Int64:
                    return sizeof(long);
                case TypeCode.SByte:
                    return sizeof(sbyte);
                case TypeCode.Single:
                    return sizeof(float);
                case TypeCode.UInt16:
                    return sizeof(ushort);
                case TypeCode.UInt32:
                    return sizeof(uint);
                case TypeCode.UInt64:
                    return sizeof(ulong);
                default:

                    return default;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ToFloat(TypeCode sourceTypeCode, void* property)
        {
            switch (sourceTypeCode)
            {
                case TypeCode.Byte:
                    return *(byte*)property;
                case TypeCode.Double:
                    return (float)*(double*)property;
                case TypeCode.Int16:
                    return *(short*)property;
                case TypeCode.Int32:
                    return *(int*)property;
                case TypeCode.Int64:
                    return *(long*)property;
                case TypeCode.SByte:
                    return *(sbyte*)property;
                case TypeCode.Single:
                    return *(float*)property;
                case TypeCode.UInt16:
                    return *(ushort*)property;
                case TypeCode.UInt32:
                    return *(uint*)property;
                case TypeCode.UInt64:
                    return *(ulong*)property;
                default:

                    return default;
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[assembly:InternalsVisibleTo("NeroWeNeed.UIECS.Manager")]
namespace NeroWeNeed.UIECS {


    public unsafe struct SizeAccessor {
        public float2* value;

        public SizeAccessor(float2* value) {
            this.value = value;
        }
    }
    public unsafe struct PositionAccessor {
        public float2* value;

        public PositionAccessor(float2* value) {
            this.value = value;
        }
    }
    public unsafe struct ConstraintAccessor {
        public float4* value;


        public ConstraintAccessor(float4* value) {
            this.value = value;
        }
    }
    internal interface IPropertyBlockField { }
    internal interface IPropertyBlockOptionalField : IPropertyBlockField { }
    public unsafe struct PropertyBlockAccessor<TPropertyBlock> : IPropertyBlockField where TPropertyBlock : unmanaged, IPropertyBlock {
        public TPropertyBlock* value;

        public PropertyBlockAccessor(TPropertyBlock* value) {
            this.value = value;
        }

    }
    public unsafe struct ExtraDataAccessor {
        public void* value;
        public long length;

        public ExtraDataAccessor(void* value, long length)
        {
            this.value = value;
            this.length = length;
        }
    }
    public unsafe struct OptionalPropertyBlockAccessor<TPropertyBlock> : IPropertyBlockOptionalField where TPropertyBlock : unmanaged, IPropertyBlock {
        public TPropertyBlock* value;
        internal byte enabled;
        public bool IsEnabled { get => enabled != 0; }

        public OptionalPropertyBlockAccessor(TPropertyBlock* value, byte enabled) {
            this.value = value;
            this.enabled = enabled;
        }
    }


    public interface IPropertyBlock {

    }

}
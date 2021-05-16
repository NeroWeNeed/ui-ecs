using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using NeroWeNeed.UIECS;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TextCore;

[assembly: RegisterGenericComponentType(typeof(UIConfigBufferExtraDataType<string>))]
namespace NeroWeNeed.UIECS
{
    public struct UISubmeshIndex : IComponentData
    {
        public int value;
        public UISubmeshIndex(int value)
        {
            this.value = value;
        }
        public static implicit operator UISubmeshIndex(int value) => new UISubmeshIndex(value);
        public static implicit operator int(UISubmeshIndex value) => value.value;
    }
    public struct UIRenderQuadCount : IComponentData
    {
        public int value;
        public UIRenderQuadCount(int value)
        {
            this.value = value;
        }

        public static implicit operator UIRenderQuadCount(int value) => new UIRenderQuadCount(value);
        public static implicit operator int(UIRenderQuadCount value) => value.value;
    }
    public struct UITotalRenderQuadCount : IComponentData
    {
        public int value;
        public UITotalRenderQuadCount(int value)
        {
            this.value = value;
        }
        public static implicit operator UITotalRenderQuadCount(int value) => new UITotalRenderQuadCount(value);
        public static implicit operator int(UITotalRenderQuadCount value) => value.value;
    }
    public struct UITotalNodeCount : IComponentData
    {
        public int value;
        public UITotalNodeCount(int value)
        {
            this.value = value;
        }
        public static implicit operator UITotalNodeCount(int value) => new UITotalNodeCount(value);
    }
    public interface IUIValueBinding
    {
        public Entity Value { get; set; }
    }
    public struct UIValueNodeComponentBinding<TComponent, TValue> : IComponentData, IUIValueBinding where TComponent : IComponentData
    {
        public Entity value;

        public Entity Value { get => value; set => this.value = value; }
    }
    public struct UIValueNodeBufferBinding<TComponent, TValue> : IComponentData, IUIValueBinding where TComponent : IBufferElementData
    {
        public Entity value;
        public Entity Value { get => value; set => this.value = value; }

        public UIValueNodeBufferBinding(Entity value)
        {
            this.value = value;
        }
    }
    [InternalBufferCapacity(256)]
    public struct UIConfigBufferData : IBufferElementData
    {
        public byte value;
        public UIConfigBufferData(byte value)
        {
            this.value = value;
        }
        public static implicit operator UIConfigBufferData(byte value) => new UIConfigBufferData(value);
        public static implicit operator byte(UIConfigBufferData value) => value.value;
    }
    [InternalBufferCapacity(0)]
    public struct UIConfigBufferExtraData : IBufferElementData
    {
        public byte value;
        public UIConfigBufferExtraData(byte value)
        {
            this.value = value;
        }
        public static implicit operator UIConfigBufferExtraData(byte value) => new UIConfigBufferExtraData(value);
        public static implicit operator byte(UIConfigBufferExtraData value) => value.value;
    }
    public struct UINodeName : IComponentData
    {
        public FixedString32 value;
        public static implicit operator UINodeName(BlobString str) => new UINodeName { value = str.ToString() };
        public static implicit operator UINodeName(string str) => new UINodeName { value = str };
    }
    [InternalBufferCapacity(4)]
    public struct UINodeClass : IBufferElementData
    {
        public FixedString32 value;
        public static implicit operator UINodeClass(BlobString str) => new UINodeClass { value = str.ToString() };
        public static implicit operator UINodeClass(string str) => new UINodeClass { value = str };
    }
    [InternalBufferCapacity(4)]
    public struct UINodeChild : IBufferElementData
    {
        public Entity value;
        public static implicit operator UINodeChild(Entity entity) => new UINodeChild { value = entity };
    }
    public struct UINodeParent : IComponentData
    {
        public Entity value;
        public static implicit operator UINodeParent(Entity entity) => new UINodeParent { value = entity };
    }
    public struct UIRootData : IComponentData
    {
        public float4 constraints;
        public Entity rootNode;
        public static UIRootData CreateDefault(Entity rootNode) => new UIRootData
        {
            constraints = new float4(0, 0, float.PositiveInfinity, float.PositiveInfinity),
            rootNode = rootNode
        };
    }
    public struct UIRoot : IComponentData, IEquatable<UIRoot>
    {
        public Entity value;
        public UIRoot(Entity value)
        {
            this.value = value;
        }
        public override bool Equals(object obj)
        {
            return obj is UIRoot root &&
                   value.Equals(root.value);
        }
        public bool Equals(UIRoot other)
        {
            return value.Equals(other.value);
        }
        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
        public static implicit operator UIRoot(Entity entity) => new UIRoot { value = entity };
        public static implicit operator Entity(UIRoot root) => root.value;
    }
    [Serializable]
    public struct UIPropertyBlockTag<TPropertyBlock> : IComponentData where TPropertyBlock : unmanaged, IPropertyBlock { }
    [Serializable]
    public struct UIElementTag<TElement> : IComponentData where TElement : unmanaged, IUINode { }
    [Serializable]
    public struct UIConfigBufferExtraDataType<T> : IComponentData { }
    [Serializable]
    public struct UINode : IComponentData { }
    public interface IUIMaterialProperty : IComponentData { }
    [InternalBufferCapacity(0)]
    public struct UIBlobAssetInfo : IBufferElementData
    {
        public Unity.Entities.Hash128 hash;
        public UnsafeUntypedBlobAssetReference blobAssetReference;

        public UIBlobAssetInfo(Unity.Entities.Hash128 hash, UnsafeUntypedBlobAssetReference blobAssetReference)
        {
            this.hash = hash;
            this.blobAssetReference = blobAssetReference;
        }
    }
    [InternalBufferCapacity(0)]
    public struct UIBlobAssetPropertyRequestInfo : IBufferElementData
    {
        public int blobIndex;
        public ulong block;
        public int offset;

        public UIBlobAssetPropertyRequestInfo(int blobIndex, ulong block, int offset)
        {
            this.blobIndex = blobIndex;
            this.block = block;
            this.offset = offset;
        }
    }
}
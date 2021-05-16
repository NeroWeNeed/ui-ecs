using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NeroWeNeed.UIECS.Jobs
{
    [BurstCompile]
    public unsafe struct NormalizationJob : IJobEntityBatch
    {
        public BufferFromEntity<UIConfigBufferData> configData;
        [ReadOnly]
        public EntityTypeHandle entityTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<UIRootData> rootDataTypeHandle;
        [ReadOnly]
        public NativeMultiHashMap<ulong, UIElementManager.UILengthInfo> normalizationInfo;
        [ReadOnly]
        public BufferFromEntity<UINodeChild> childrenData;
        public float2 viewportSize;
        public float dpi;
        public float pixelScale;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var entities = batchInChunk.GetNativeArray(entityTypeHandle);
            var roots = batchInChunk.GetNativeArray(rootDataTypeHandle);
            var context = new SimpleUILengthContext(dpi, pixelScale, viewportSize);
            for (int i = 0; i < batchInChunk.Count; i++)
            {
                var entity = roots[i].rootNode;
                var handle = configData[entity].GetHandle();
                Normalize(entity, handle, default, handle, ref context, ReferencePropertyTarget.Self | ReferencePropertyTarget.Root);
            }
        }
        public void Normalize(Entity entity, UIConfigHandle root, UIConfigHandle parent, UIConfigHandle self, ref SimpleUILengthContext context, ReferencePropertyTarget supported)
        {
            foreach (var block in self)
            {
                if (normalizationInfo.TryGetFirstValue(block.header->hash, out var lengthInfo, out NativeMultiHashMapIterator<ulong> iterator))
                {
                    do
                    {

                        UILength* length = (UILength*)(((byte*)block.data) + lengthInfo.property.offset);

                        UIConfigHandle referenceHandle = default;
                        switch (lengthInfo.target)
                        {
                            case ReferencePropertyTarget.Self:
                                referenceHandle = self;
                                break;
                            case ReferencePropertyTarget.Parent:
                                referenceHandle = parent;
                                break;
                            case ReferencePropertyTarget.Root:
                                referenceHandle = root;
                                break;
                        }
                        if (lengthInfo.target == ReferencePropertyTarget.None || !lengthInfo.referenceProperty.IsCreated || !referenceHandle.TryGetProperty(lengthInfo.referenceProperty, out UILength* referenceValue) || (((byte)lengthInfo.target) & ((byte)supported)) == 0)
                        {
                            context.RelativeTo = 0f;
                        }
                        else
                        {
                            context.RelativeTo = referenceValue->realValue;
                        }
                        length->realValue = length->Normalize(context);
                    } while (normalizationInfo.TryGetNextValue(out lengthInfo, ref iterator));
                }
            }
            foreach (var child in childrenData[entity])
            {
                Normalize(child.value, root, self, configData[child.value].GetHandle(), ref context, ReferencePropertyTarget.Self | ReferencePropertyTarget.Parent | ReferencePropertyTarget.Root);
            }
        }
    }
}
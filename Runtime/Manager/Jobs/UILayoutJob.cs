using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static NeroWeNeed.UIECS.UIElementManager;

namespace NeroWeNeed.UIECS.Jobs
{
    [BurstCompile]
    public unsafe struct UILayoutJob : IJobEntityBatchWithIndex
    {
        public BufferFromEntity<UIConfigBufferData> configData;
        [ReadOnly]
        public BufferFromEntity<UIConfigBufferExtraData> extraData;
        [ReadOnly]
        public EntityTypeHandle entityHandle;
        [ReadOnly]
        public ComponentTypeHandle<UIRootData> rootHandle;
        [ReadOnly]
        public BufferFromEntity<UINodeChild> nodeData;
        [ReadOnly]
        public NativeHashMap<ulong, UIElementInfo> elementInfo;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex, int indexOfFirstEntityInQuery)
        {
            var entities = batchInChunk.GetNativeArray(entityHandle);
            var roots = batchInChunk.GetNativeArray(rootHandle);
            for (int i = 0; i < batchInChunk.Count; i++)
            {
                var root = roots[i];
                var configHandle = configData[root.rootNode].GetHandle();
                var extraData = this.extraData[root.rootNode];
                var extraDataHandle = extraData.GetReadOnlyHandle();
                configHandle.SetConstraints(roots[i].constraints);
                if (configHandle.GetPropertyBlock<DisplayConfigBlock>()->Display)
                {
                    Layout(roots[i].rootNode, configHandle, extraDataHandle, extraData.Length);
                }
                else
                {
                    Clear(roots[i].rootNode, configHandle);
                }
            }
        }
        public void Layout(Entity entity, UIConfigHandle configHandle, UIExtraDataHandle extraDataHandle, long extraDataHandleLength)
        {
            var element = configHandle.Element();
            var elementInfo = this.elementInfo[element];
            if (!elementInfo.IsTerminal)
            {
                var childrenEntities = nodeData[entity].Reinterpret<Entity>();
                var childrenConfigHandles = new NativeArray<UIConfigHandle>(childrenEntities.Length, Allocator.Temp);
                var childrenExtraDataHandleInfo = new NativeArray<ValueTuple<UIExtraDataHandle, long>>(childrenEntities.Length, Allocator.Temp);
                NativeBitArray shouldDisplay = new NativeBitArray(childrenEntities.Length, Allocator.Temp);
                for (int i = 0; i < childrenEntities.Length; i++)
                {
                    var childConfigHandle = configData[childrenEntities[i]].GetHandle();
                    var childExtraData = extraData[childrenEntities[i]];
                    var childExtraDataHandle = childExtraData.GetReadOnlyHandle();
                    childrenConfigHandles[i] = childConfigHandle;
                    childrenExtraDataHandleInfo[i] = (childExtraDataHandle, childExtraData.Length);
                    shouldDisplay.Set(i, childConfigHandle.GetPropertyBlock<DisplayConfigBlock>()->Display);
                }
                var childrenHandlePointer = (UIConfigHandle*)childrenConfigHandles.GetUnsafePtr();
                for (int i = 0; i < childrenEntities.Length; i++)
                {

                    elementInfo.constrain.Invoke(configHandle, childrenHandlePointer, i, childrenConfigHandles.Length, out float4 constraints, extraDataHandle, extraDataHandleLength);
                    childrenConfigHandles[i].SetConstraints(constraints);
                    if (shouldDisplay.IsSet(i))
                    {
                        Layout(childrenEntities[i], childrenConfigHandles[i],childrenExtraDataHandleInfo[i].Item1,childrenExtraDataHandleInfo[i].Item2);
                    }
                    else
                    {
                        Clear(childrenEntities[i], childrenConfigHandles[i]);
                    }
                }
                if (childrenConfigHandles.Length > 0)
                {
                    var positions = new NativeArray<float2>(childrenConfigHandles.Length, Allocator.Temp);

                    elementInfo.layout.Invoke(configHandle, childrenHandlePointer, childrenConfigHandles.Length, (float2*)positions.GetUnsafePtr(), extraDataHandle, extraDataHandleLength);
                    for (int i = 0; i < childrenConfigHandles.Length; i++)
                    {
                        if (shouldDisplay.IsSet(i))
                        {
                            childrenConfigHandles[i].SetPosition(positions[i]);
                            //ltwData[childrenEntities[i]] = new LocalToWorld { Value = elementLtw + float4x4.Translate(new float3(positions[i], 0.1f)) };


                        }
                    }
                    positions.Dispose();
                }
                elementInfo.size.Invoke(configHandle, childrenHandlePointer, childrenConfigHandles.Length, out float2 size, extraDataHandle, extraDataHandleLength);
                configHandle.SetSize(size);
                shouldDisplay.Dispose();
                childrenConfigHandles.Dispose();
                childrenExtraDataHandleInfo.Dispose();
            }
            else
            {
                elementInfo.size.Invoke(configHandle, (UIConfigHandle*)IntPtr.Zero, 0, out float2 size, extraDataHandle, extraDataHandleLength);
                configHandle.SetSize(size);
            }



        }
        private void Clear(Entity entity, UIConfigHandle handle)
        {
            handle.SetSize(float2.zero);
            handle.SetPosition(float2.zero);
            var childrenEntities = nodeData[entity].Reinterpret<Entity>();
            for (int i = 0; i < childrenEntities.Length; i++)
            {
                Clear(childrenEntities[i], configData[childrenEntities[i]].GetHandle());
            }
        }
    }
}
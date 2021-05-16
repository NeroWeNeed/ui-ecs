using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using static NeroWeNeed.UIECS.UIElementManager;
using static UnityEngine.Mesh;
namespace NeroWeNeed.UIECS.Jobs
{
    
    [BurstCompile]
    public unsafe struct UIRenderJob : IJobEntityBatchWithIndex
    {
        [ReadOnly]
        public BufferFromEntity<UIConfigBufferData> configData;
        [ReadOnly]
        public BufferFromEntity<UIConfigBufferExtraData> extraData;
        [ReadOnly]
        public ComponentTypeHandle<UITotalRenderQuadCount> totalRenderQuadCountHandle;
        [ReadOnly]
        public ComponentDataFromEntity<UIRenderQuadCount> renderQuadCountData;
        [ReadOnly]
        public ComponentTypeHandle<UITotalNodeCount> totalNodeCountHandle;

        [ReadOnly]
        public BufferFromEntity<UINodeChild> nodeData;
        public MeshDataArray meshDataArray;
        public ComponentDataFromEntity<RenderBounds> renderBoundsData;
        [ReadOnly]
        public NativeHashMap<ulong, UIElementInfo> elementInfo;
        public ComponentTypeHandle<UIRootData> rootDataTypeHandle;
        [DeallocateOnJobCompletion]
        public NativeArray<VertexAttributeDescriptor> vertexAttributeData;
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex, int indexOfFirstEntityInQuery)
        {
            var roots = batchInChunk.GetNativeArray(rootDataTypeHandle);
            var totalRenderQuads = batchInChunk.GetNativeArray(totalRenderQuadCountHandle);
            var totalNodeCount = batchInChunk.GetNativeArray(totalNodeCountHandle);
            for (int i = 0; i < batchInChunk.Count; i++)
            {
                var meshData = meshDataArray[i];
                meshData.SetVertexBufferParams(totalRenderQuads[i].value * 4, vertexAttributeData);
                meshData.SetIndexBufferParams(totalRenderQuads[i].value * 6, UnityEngine.Rendering.IndexFormat.UInt16);
                meshData.subMeshCount = totalNodeCount[i].value;

                var vertexData = meshData.GetVertexData<UIVertexData>();
                var indexData = meshData.GetIndexData<ushort>();
                int offset = 0;
                int subMesh = 0;
                Render(roots[i].rootNode, float2.zero, ref offset, ref subMesh, ref vertexData, ref indexData, ref meshData, true);
            }
        }
        private void Render(Entity entity,
        float2 positionOffset,
        ref int dataOffset,
        ref int subMesh,
        ref NativeArray<UIVertexData> vertexData,
        ref NativeArray<ushort> indexData,
        ref MeshData meshData,
        bool visible
        )
        {
            var configHandle = configData[entity].GetReadOnlyHandle();
            var extraData = this.extraData[entity];
            var extraDataHandle = extraData.GetReadOnlyHandle();
            var displayBlock = configHandle.GetPropertyBlock<DisplayConfigBlock>();

            configHandle.GetPosition(out float2 position);
            configHandle.GetSize(out float2 size);
            var renderQuadCount = renderQuadCountData[entity];
            var element = configHandle.Element();
            var elementInfo = this.elementInfo[element];
            var elementVertexData = (UIVertexData*)vertexData.GetSubArray(dataOffset * 4, renderQuadCount * 4).GetUnsafePtr();
            if (displayBlock->Visible && visible)
            {
                elementInfo.generateMeshData.Invoke(configHandle, new float4(positionOffset + position, positionOffset + position + size), elementVertexData, extraDataHandle, extraData.Length);
            }
            else
            {
                UnsafeUtility.MemClear(elementVertexData, UnsafeUtility.SizeOf<UIVertexData>() * renderQuadCount * 4);
            }
            for (int i = 0; i < renderQuadCount; i++)
            {
                indexData[(dataOffset + i) * 6] = (ushort)((dataOffset + i) * 4);
                indexData[((dataOffset + i) * 6) + 1] = (ushort)(((dataOffset + i) * 4) + 1);
                indexData[((dataOffset + i) * 6) + 2] = (ushort)(((dataOffset + i) * 4) + 2);
                indexData[((dataOffset + i) * 6) + 3] = (ushort)(((dataOffset + i) * 4) + 1);
                indexData[((dataOffset + i) * 6) + 4] = (ushort)(((dataOffset + i) * 4) + 3);
                indexData[((dataOffset + i) * 6) + 5] = (ushort)(((dataOffset + i) * 4) + 2);
            }
            Bounds bounds = new Bounds(new float3(((size / 2f) + position) * UIVertexData.flipVector, 0), new float3(size, 0));
            var sm = new SubMeshDescriptor(dataOffset * 6, renderQuadCount * 6, MeshTopology.Triangles)
            {
                bounds = bounds,
                firstVertex = dataOffset * 4,
                vertexCount = renderQuadCount * 4
            };

            meshData.SetSubMesh(subMesh++, sm);
            renderBoundsData[entity] = new RenderBounds { Value = bounds.ToAABB() };
            var children = nodeData[entity];
            dataOffset += renderQuadCount;
            for (int i = 0; i < children.Length; i++)
            {
                Render(children[i].value, positionOffset + position, ref dataOffset, ref subMesh, ref vertexData, ref indexData, ref meshData, displayBlock->Visible && visible);
            }
        }
    }
}



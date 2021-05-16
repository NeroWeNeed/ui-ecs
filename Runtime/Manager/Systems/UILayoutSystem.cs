using System.Collections.Generic;
using System.Linq;
using NeroWeNeed.UIECS.Jobs;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeroWeNeed.UIECS.Systems
{

    [UpdateInGroup(typeof(UISystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public class UILayoutSystem : SystemBase
    {
        private EntityQuery query;
        private List<Mesh> meshBuffer;
        protected override void OnCreate()
        {
            base.OnCreate();
            query = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] {
                    ComponentType.ReadOnly<UIRootData>(), ComponentType.ReadWrite<UITotalRenderQuadCount>(), ComponentType.ReadWrite<UITotalNodeCount>()
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
#if UNITY_EDITOR
            UIElementManager.InitializeFunctionPointers();
#endif
            meshBuffer = new List<Mesh>();
        }
        protected override void OnUpdate()
        {
            var uiObjectCount = query.CalculateEntityCount();
            var meshDataArray = Mesh.AllocateWritableMeshData(uiObjectCount);
            var rootEntityData = query.ToComponentDataArray<UIRootData>(Unity.Collections.Allocator.Temp);
            meshBuffer.Clear();
            for (int i = 0; i < rootEntityData.Length; i++)
            {
                meshBuffer.Add(EntityManager.GetSharedComponentData<RenderMesh>(rootEntityData[i].rootNode).mesh);
            }
            var initMeshDataJobHandle = new UIInitMeshDataJob
            {
                totalRenderQuadCountHandle = GetComponentTypeHandle<UITotalRenderQuadCount>(),
                rootDataTypeHandle = GetComponentTypeHandle<UIRootData>(),
                renderQuadCountData = GetComponentDataFromEntity<UIRenderQuadCount>(true),
                childrenData = GetBufferFromEntity<UINodeChild>(true)
            }.Schedule(query);
            var layoutJobHandle = new UILayoutJob
            {
                configData = GetBufferFromEntity<UIConfigBufferData>(),
                extraData = GetBufferFromEntity<UIConfigBufferExtraData>(true),
                entityHandle = GetEntityTypeHandle(),
                rootHandle = GetComponentTypeHandle<UIRootData>(true),
                nodeData = GetBufferFromEntity<UINodeChild>(true),
                elementInfo = UIElementManager.elementInfo
            }.Schedule(query, initMeshDataJobHandle);
            var renderJobHandle = new UIRenderJob
            {
                configData = GetBufferFromEntity<UIConfigBufferData>(true),
                extraData = GetBufferFromEntity<UIConfigBufferExtraData>(true),
                renderQuadCountData = GetComponentDataFromEntity<UIRenderQuadCount>(true),
                totalRenderQuadCountHandle = GetComponentTypeHandle<UITotalRenderQuadCount>(true),
                totalNodeCountHandle = GetComponentTypeHandle<UITotalNodeCount>(true),
                nodeData = GetBufferFromEntity<UINodeChild>(true),
                meshDataArray = meshDataArray,
                elementInfo = UIElementManager.elementInfo,
                rootDataTypeHandle = GetComponentTypeHandle<UIRootData>(),
                renderBoundsData = GetComponentDataFromEntity<RenderBounds>(),
                vertexAttributeData = UIVertexData.AllocateVertexDescriptor()
            }.Schedule(query, layoutJobHandle);
            renderJobHandle.Complete();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, meshBuffer);
            foreach (var mesh in meshBuffer)
            {
                mesh.RecalculateBounds();
            }
        }
    }
}
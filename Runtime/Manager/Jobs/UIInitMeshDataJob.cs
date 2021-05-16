using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace NeroWeNeed.UIECS.Jobs
{
    [BurstCompile]
    public unsafe struct UIInitMeshDataJob : IJobEntityBatchWithIndex
    {
        public ComponentTypeHandle<UITotalRenderQuadCount> totalRenderQuadCountHandle;
        [ReadOnly]
        public ComponentTypeHandle<UIRootData> rootDataTypeHandle;
        [ReadOnly]
        public ComponentDataFromEntity<UIRenderQuadCount> renderQuadCountData;
        [ReadOnly]
        public BufferFromEntity<UINodeChild> childrenData;
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex, int indexOfFirstEntityInQuery)
        {
            var totalRenderQuadCount = batchInChunk.GetNativeArray(totalRenderQuadCountHandle);
            var roots = batchInChunk.GetNativeArray(rootDataTypeHandle);
            for (int i = 0; i < batchInChunk.Count; i++)
            {
                int total = 0;
                int submesh = 0;
                CountRenderQuads(roots[i].rootNode, ref total, ref submesh);
                totalRenderQuadCount[i] = total;
            }
        }
        private void CountRenderQuads(Entity entity, ref int totalQuads, ref int submesh)
        {

            totalQuads += renderQuadCountData[entity].value;
            var children = childrenData[entity];
            for (int i = 0; i < children.Length; i++)
            {
                CountRenderQuads(children[i].value, ref totalQuads, ref submesh);
            }


        }
    }
}
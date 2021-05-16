using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using UnityEngine;

namespace NeroWeNeed.UIECS.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public class UIBlobAssetUpdateSystem : SystemBase
    {
        private EntityQuery query;
        //private List<UIRoot> rootBuffer = new List<UIRoot>();
        protected override void OnCreate()
        {
            query = GetEntityQuery(ComponentType.ReadOnly<UIBlobAssetPropertyRequestInfo>(), ComponentType.ReadWrite<UIConfigBufferData>(), ComponentType.ReadOnly<UIRoot>());
            query.SetChangedVersionFilter(ComponentType.ReadOnly<UIBlobAssetPropertyRequestInfo>());
        }
        protected unsafe override void OnUpdate()
        {
/*             rootBuffer.Clear();
            EntityManager.GetAllUniqueSharedComponentData<UIRoot>(rootBuffer);
            
            foreach (var item in rootBuffer)
            {
                Debug.Log(item.value);
            } */
            new UpdateBlobAssetUIPropertyJob
            {
                blobAssetData = GetBufferFromEntity<UIBlobAssetInfo>(true),
                configHandle = GetBufferTypeHandle<UIConfigBufferData>(),
                blobPropertyInfo = GetBufferTypeHandle<UIBlobAssetPropertyRequestInfo>(true),
                rootHandle = GetComponentTypeHandle<UIRoot>()
                //roots = new NativeArray<UIRoot>(rootBuffer.ToArray(), Allocator.TempJob)
            }.ScheduleParallel(query).Complete();
        }
        internal unsafe struct UpdateBlobAssetUIPropertyJob : IJobEntityBatch
        {
            [ReadOnly]
            public BufferFromEntity<UIBlobAssetInfo> blobAssetData;
            public BufferTypeHandle<UIConfigBufferData> configHandle;
            [ReadOnly]
            public BufferTypeHandle<UIBlobAssetPropertyRequestInfo> blobPropertyInfo;
            [ReadOnly]
            public ComponentTypeHandle<UIRoot> rootHandle;

/*             [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<UIRoot> roots; */
            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var configs = batchInChunk.GetBufferAccessor(configHandle);
                var blobRequests = batchInChunk.GetBufferAccessor(blobPropertyInfo);
                var roots = batchInChunk.GetNativeArray(rootHandle);

                for (int i = 0; i < batchInChunk.Count; i++)
                {
                    var configHandle = configs[i].GetHandle();
                    var rootBlobAssets = blobAssetData[roots[i].value];
                    var blobRequest = blobRequests[i];
                    for (int j = 0; j < blobRequest.Length; j++)
                    {
                        var info = blobRequest[j];
                        var propertyBlock = (IntPtr)configHandle.GetPropertyBlockFromHash(info.block);
                        var blob = rootBlobAssets[info.blobIndex].blobAssetReference;


                        UnsafeUtility.MemCpy((propertyBlock + info.offset).ToPointer(), UnsafeUtility.AddressOf(ref blob), UnsafeUtility.SizeOf<UnsafeUntypedBlobAssetReference>());
                    }
                }

            }
        }
    }
}
using System.Runtime.CompilerServices;
using NeroWeNeed.UIECS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace NeroWeNeed.UIECS.Systems {
    [UpdateInGroup(typeof(UISystemGroup))]
    [UpdateBefore(typeof(UILayoutSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public unsafe class UILengthNormalizationSystem : SystemBase {
        private EntityQuery query;
        protected override void OnCreate() {
            //query = GetEntityQuery(ComponentType.ReadOnly<UIRootData>(), ComponentType.ReadWrite<UIConfigBufferData>(), ComponentType.ReadOnly<UINode>(),ComponentType.ReadOnly<UIRoot>());
            query = GetEntityQuery(ComponentType.ReadOnly<UIRootData>(), ComponentType.ReadWrite<UITotalRenderQuadCount>(), ComponentType.ReadWrite<UITotalNodeCount>());
        }
        protected override void OnUpdate() {
            new NormalizationJob
            {
                configData = GetBufferFromEntity<UIConfigBufferData>(),
                entityTypeHandle = GetEntityTypeHandle(),
                childrenData = GetBufferFromEntity<UINodeChild>(true),
                normalizationInfo = UIElementManager.uiLengthNormalizationInfo,
                pixelScale = 0.1f,
                dpi = 96,
                viewportSize = float2.zero,
                rootDataTypeHandle = GetComponentTypeHandle<UIRootData>()
            }.Schedule(query).Complete();
        }


    }
}
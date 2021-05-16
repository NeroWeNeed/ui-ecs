using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace NeroWeNeed.UIECS.Systems
{
/* 
    [UpdateInGroup(typeof(UISystemGroup))]
    [UpdateAfter(typeof(UILayoutSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public class UIPositionWriteUpdateSystem : SystemBase
    {
        private EntityQuery query;
        protected override void OnCreate()
        {
            base.OnCreate();
            query = GetEntityQuery(ComponentType.ReadOnly<UIRootData>(), ComponentType.ReadOnly<LocalToWorld>());
        }
        protected override void OnUpdate()
        {
            var count = query.CalculateEntityCount();
            var rootLtw = query.ToComponentDataArray<LocalToWorld>(Unity.Collections.Allocator.Temp);
            var rootEntities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            var mapping = new NativeHashMap<Entity, LocalToWorld>(count, Allocator.Temp);
            for (int i = 0; i < count; i++)
            {
                mapping[rootEntities[i]] = rootLtw[i];
            }
            Entities.WithAll<UINode>().ForEach((ref LocalToWorld ltw, in UINodePosition position, in UIRoot root) =>
            {
                var rootLtw = mapping[root.value];
                ltw = new LocalToWorld { Value = float4x4.TRS(new float3(position * UIVertexData.flipVector, 0f) + rootLtw.Position, rootLtw.Rotation, 1) };
            }).WithoutBurst().Run();
        }
    } */
    /*     [UpdateInGroup(typeof(UISystemGroup))]
        [UpdateAfter(typeof(UILayoutSystem))]
        public class UIPositionUpdateSystem : SystemBase
        {
            //private EntityQuery query;
            protected override void OnCreate()
            {
                base.OnCreate();
                query = GetEntityQuery(
                    ComponentType.ReadWrite<LocalToWorld>(),
                     ComponentType.ReadOnly<UINode>(), 
                     ComponentType.ReadOnly<UINodePosition>(), 
                     ComponentType.ReadOnly<UIConfigBufferData>(),
                      ComponentType.ReadOnly<UIRoot>(), 
                      ComponentType.Exclude<UIRootData>());
            }
            protected override void OnUpdate()
            {
                Entities.WithAll<UINode>().ForEach((ref LocalToWorld ltw, in UIRoot root, in UINodePosition position) =>
                {
                    var rootLtw = GetComponent<LocalToWorld>(root.value);
                    var rootRotation = rootLtw.Rotation;
                    ltw.Value = rootLtw.Value + float4x4.Translate((math.right() * position.X) + (math.down() * position.Y));
                }).ScheduleParallel();
            }
        } */
}
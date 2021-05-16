using System;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine.TextCore;

namespace NeroWeNeed.UIECS.Systems
{
    /*     [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
        [UpdateInGroup(typeof(UISystemGroup))]
        [UpdateBefore(typeof(UILayoutSystem))]
        public class UITextProcessingSystemGroup : ComponentSystemGroup { }
        [UpdateInGroup(typeof(UIInitializationSystemGroup))]
        [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
        public class UITextInitializationSystem : SystemBase
        {
            private EntityCommandBufferSystem entityCommandBufferSystem;
            protected override void OnCreate()
            {
                entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            }
            protected unsafe override void OnUpdate()
            {
                var ecb = entityCommandBufferSystem.CreateCommandBuffer();
                Entities.WithNone<UITextSystemData>().ForEach((Entity entity, DynamicBuffer<UITextData> textData) =>
                {

                    if (textData.Length > 0)
                    {
                        var ptr = UnsafeUtility.Malloc(textData.Length, 0, Unity.Collections.Allocator.Persistent);
                        UnsafeUtility.MemCpy(ptr, textData.GetUnsafePtr(), textData.Length);
                        ecb.AddComponent(entity, new UITextSystemData
                        {
                            value = ptr,
                            allocatedLength = textData.Length,
                            length = textData.Length
                        });
                    }
                    else
                    {

                        ecb.AddComponent(entity, new UITextSystemData
                        {
                            value = IntPtr.Zero.ToPointer(),
                            allocatedLength = 0,
                            length = 0
                        });
                    }
                }).WithoutBurst().Run();
                Entities.WithNone<UITextData>().ForEach((Entity entity, in UITextSystemData data) =>
                 {
                     if (data.IsCreated)
                     {
                         UnsafeUtility.Free(data.value, Unity.Collections.Allocator.Persistent);
                     }
                     ecb.RemoveComponent<UITextSystemData>(entity);
                 }).WithoutBurst().Run();
            }
            protected unsafe override void OnDestroy()
            {
                Entities.ForEach((Entity entity, in UITextSystemData data) =>
                {
                    if (data.IsCreated)
                    {
                        UnsafeUtility.Free(data.value, Unity.Collections.Allocator.Persistent);
                        EntityManager.RemoveComponent<UITextSystemData>(entity);
                    }
                }).WithStructuralChanges().WithoutBurst().Run();
            }
        }
        [UpdateInGroup(typeof(UIInitializationSystemGroup))]
        [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
        public class UIGlyphInitializationSystem : SystemBase
        {
            private EntityCommandBufferSystem entityCommandBufferSystem;
            protected override void OnCreate()
            {
                entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            }
            protected unsafe override void OnUpdate()
            {
                var ecb = entityCommandBufferSystem.CreateCommandBuffer();
                Entities.WithNone<UIGlyphSystemData>().ForEach((Entity entity, DynamicBuffer<UITextData> textData) =>
                {

                    if (textData.Length > 0)
                    {
                        var allocatedLength = (textData.Length / 2) * UnsafeUtility.SizeOf<UIGlyph>();
                        var ptr = UnsafeUtility.Malloc(allocatedLength, 0, Unity.Collections.Allocator.Persistent);
                        ecb.AddComponent(entity, new UIGlyphSystemData
                        {
                            value = ptr,
                            allocatedLength = allocatedLength
                        });
                    }
                    else
                    {

                        ecb.AddComponent(entity, new UIGlyphSystemData
                        {
                            value = IntPtr.Zero.ToPointer(),
                            allocatedLength = 0
                        });
                    }
                }).WithoutBurst().Run();
                Entities.WithNone<UITextData>().ForEach((Entity entity, in UIGlyphSystemData data) =>
                 {
                     if (data.IsCreated)
                     {
                         UnsafeUtility.Free(data.value, Unity.Collections.Allocator.Persistent);
                     }
                     ecb.RemoveComponent<UIGlyphSystemData>(entity);
                 }).WithoutBurst().Run();
            }
            protected unsafe override void OnDestroy()
            {
                Entities.ForEach((Entity entity, in UIGlyphSystemData data) =>
                {
                    if (data.IsCreated)
                    {
                        UnsafeUtility.Free(data.value, Unity.Collections.Allocator.Persistent);
                        EntityManager.RemoveComponent<UIGlyphSystemData>(entity);
                    }
                }).WithStructuralChanges().WithoutBurst().Run();
            }
        }
        [UpdateInGroup(typeof(UITextProcessingSystemGroup))]
        [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
        public unsafe class UITextUpdateSystem : SystemBase
        {
            protected override void OnUpdate()
            {
                Entities.WithChangeFilter<UITextData>().ForEach((ref DynamicBuffer<UITextData> data, ref UITextSystemData systemData) =>
                {
                    var length = data.Length;
                    if (data.Length != 0 && systemData.allocatedLength != 0)
                    {
                        if (systemData.allocatedLength < length)
                        {
                            if (systemData.IsCreated)
                            {
                                UnsafeUtility.Free(systemData.value, Unity.Collections.Allocator.Persistent);
                            }
                            systemData.value = UnsafeUtility.Malloc(length, 0, Unity.Collections.Allocator.Persistent);
                            systemData.length = length;
                            systemData.allocatedLength = length;
                        }
                        else
                        {
                            UnsafeUtility.MemCpy(systemData.value, data.GetUnsafePtr(), data.Length);
                            systemData.length = length;
                        }
                    }
                }).Schedule();
            }
        }
        [UpdateInGroup(typeof(UITextProcessingSystemGroup))]
        [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
        public unsafe class UITextGlyphProcessingSystem : SystemBase
        {
            private EntityQuery query;
            protected override void OnCreate()
            {
                base.OnCreate();
                query = GetEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[] {
                        ComponentType.ReadOnly<UIAssetComponent<FontConfigBlock, TMP_FontAsset>>(),
                        ComponentType.ReadOnly<UITextSystemData>(),
                        ComponentType.ReadWrite<UIGlyphSystemData>()
                    }
                });
                query.AddChangedVersionFilter(ComponentType.ReadOnly<UITextSystemData>());
                RequireForUpdate(query);
            }
            protected override void OnUpdate()
            {

                var chunks = query.CreateArchetypeChunkArray(Unity.Collections.Allocator.TempJob);
                if (chunks.Length > 0)
                {
                    UpdateGlyphs(chunks, chunks.Length);
                }
                chunks.Dispose();
            }
            private void UpdateGlyphs(NativeArray<ArchetypeChunk> chunks, int chunkCount)
            {
                var fontHandle = GetSharedComponentTypeHandle<UIAssetComponent<FontConfigBlock, TMP_FontAsset>>();
                var textHandle = GetComponentTypeHandle<UITextSystemData>(true);
                var glyphHandle = GetComponentTypeHandle<UIGlyphSystemData>();
                for (int i = 0; i < chunkCount; i++)
                {
                    var chunk = chunks[i];
                    var chunkFont = chunk.GetSharedComponentData(fontHandle, EntityManager);
                    var chunkTextData = chunk.GetNativeArray(textHandle);
                    var chunkGlyphData = chunk.GetNativeArray(glyphHandle);
                    for (int j = 0; j < chunk.Count; j++)
                    {
                        var textData = chunkTextData[j];
                        if (textData.IsCreated)
                        {
                            var glyphData = chunkGlyphData[j];
                            for (int k = 0; k < textData.CharCount; k++)
                            {
                                uint current = UnsafeUtility.ReadArrayElement<ushort>(textData.value, k);
                                var glyph = chunkFont.value.glyphLookupTable[current];
                                UnsafeUtility.WriteArrayElement(glyphData.value, k, new UIGlyph
                                {
                                    uv = new Unity.Mathematics.float4(
                                        glyph.glyphRect.x / ((float)chunkFont.value.atlasWidth),
                                        glyph.glyphRect.y / ((float)chunkFont.value.atlasHeight),
                                        (glyph.glyphRect.x + glyph.glyphRect.width) / ((float)chunkFont.value.atlasWidth),
                                        (glyph.glyphRect.y + glyph.glyphRect.height) / ((float)chunkFont.value.atlasHeight)),
                                    size = new Unity.Mathematics.float2(
                                            glyph.metrics.width,
                                            glyph.metrics.height
                                        ),
                                    horizontalBearing = new Unity.Mathematics.float2(
                                            glyph.metrics.horizontalBearingX, glyph.metrics.horizontalBearingY
                                        ),
                                    horizontalAdvance = glyph.metrics.horizontalAdvance,
                                    scale = glyph.scale
                                });
                            }
                        }
                    }

                }
            }
        }
        [UpdateInGroup(typeof(UITextProcessingSystemGroup))]
        [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
        public class UITextRenderQuadCountUpdateSystem : SystemBase
        {
            protected override void OnUpdate()
            {
                Entities.ForEach((ref UIRenderQuadCount quadCount, in UITextSystemData data) => quadCount.value = data.CharCount > 0 ? (data.CharCount + 1) : 0).ScheduleParallel(Dependency).Complete();
            }
        }
        [UpdateInGroup(typeof(UITextProcessingSystemGroup))]
        [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
        public unsafe class UITextNodeConfigBufferUpdate : SystemBase
        {
            protected override void OnUpdate()
            {
                Entities.WithChangeFilter<UITextSystemData, UIGlyphSystemData>().ForEach((ref DynamicBuffer<UIConfigBufferData> config, in UITextSystemData textData, in UIGlyphSystemData glyphData) =>
                 {
                     config.GetHandle().GetPropertyBlock<TextConfigBlock>()->text = new UIText
                     {
                         allocatedCharacterDataLength = textData.allocatedLength,
                         allocatedGlyphDataLength = glyphData.allocatedLength,
                         characterData = (ushort*)textData.value,
                         glyphData = (UIGlyph*)glyphData.value,
                         characterDataLength = textData.length
                     };
                 }).ScheduleParallel(Dependency).Complete();
            }
        } */
    [UpdateInGroup(typeof(UISystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateBefore(typeof(UILengthNormalizationSystem))]
    public class UITextRenderQuadCountUpdateSystem : SystemBase
    {
        private EntityQuery query;
        protected override void OnCreate()
        {
            base.OnCreate();
            query = GetEntityQuery(ComponentType.ReadOnly<UIPropertyBlockTag<TextConfigBlock>>(), ComponentType.ReadWrite<UIRenderQuadCount>(), ComponentType.ReadOnly<UIConfigBufferExtraData>());
        }
        protected override void OnUpdate()
        {
            new CountQuadJob
            {
                extraDataTypeHandle = GetBufferTypeHandle<UIConfigBufferExtraData>(true),
                renderQuadCountTypeHandle = GetComponentTypeHandle<UIRenderQuadCount>()
            }.ScheduleParallel(query).Complete();
            
        }
        internal struct CountQuadJob : IJobEntityBatch
        {
            [ReadOnly]
            public BufferTypeHandle<UIConfigBufferExtraData> extraDataTypeHandle;
            public ComponentTypeHandle<UIRenderQuadCount> renderQuadCountTypeHandle;
            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var extraDataBuffers = batchInChunk.GetBufferAccessor(extraDataTypeHandle);
                var renderQuadCounts = batchInChunk.GetNativeArray(renderQuadCountTypeHandle);
                for (int i = 0; i < batchInChunk.Count; i++)
                {
                    var extraData = extraDataBuffers[i];
                    var length = extraData.GetReadOnlyHandle().GetTextLength();
                    renderQuadCounts[i] = length;
                }
            }
        }
    }
}
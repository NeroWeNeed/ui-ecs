using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NeroWeNeed.Commons.Editor;
using NeroWeNeed.UIECS.Authoring;
using NeroWeNeed.UIECS.Editor;
using NeroWeNeed.UIECS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;

namespace NeroWeNeed.UIECS.Systems
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.GameObjectConversion)]
    public class UIObjectConversionSystem : GameObjectConversionSystem
    {
        private readonly List<BlobAssetRequest> blobAssetRequestBuffer = new List<BlobAssetRequest>();
        protected unsafe override void OnUpdate()
        {
            Profiler.BeginSample("Convering UI Object");
            Entities.ForEach((Entity entity, UIObject uiObject) =>
            {
                if (uiObject.view != null)
                {
                    DeclareAssetDependency(uiObject.gameObject, uiObject.view);
                    //uiObject.view.ToList(this.nodeBuffer);
                    var rootEntity = GetPrimaryEntity(uiObject);
                    var nodes = new NativeArray<Entity>(uiObject.view.NodeCount, Allocator.Temp);
                    if (uiObject.mesh == null)
                    {
                        uiObject.mesh = new Mesh
                        {
                            name = "Mesh",
                            bounds = new Bounds(float3.zero, float3.zero)
                        };
                    }
                    uiObject.mesh.subMeshCount = uiObject.view.NodeCount;
                    var baseName = uiObject.gameObject.name;
                    DeclareAssetDependency(uiObject.gameObject, uiObject.view.group.Material);
                    UIConversionComponentManager.ModelInfo modelInfo = uiObject.model != null ? UIConversionComponentManager.GetModelInfo(uiObject.model.modelName) : null;
                    if (nodes.Length > 0)
                    {
                        CreateAdditionalEntity(uiObject, nodes);
                        DstEntityManager.AddComponentData(rootEntity, UIRootData.CreateDefault(nodes[0]));
                        DstEntityManager.AddComponent<UITotalRenderQuadCount>(rootEntity);
                        DstEntityManager.AddComponentData<UITotalNodeCount>(rootEntity, nodes.Length);
                        if (modelInfo != null)
                        {
                            DeclareAssetDependency(uiObject.gameObject, uiObject.model);
                            foreach (var component in modelInfo.components)
                            {
                                DstEntityManager.AddComponent(nodes[0], component);
                                UIConversionComponentManager.InitializePropertyComponents(component, DstEntityManager, 0, uiObject.view, nodes);
                            }
                        }
                    }
                    BlobData rootBlobInfo = EntityManager.HasComponent<BlobData>(entity) ? EntityManager.GetComponentObject<BlobData>(entity) : null;

                    for (int i = 0; i < nodes.Length; i++)
                    {
                        blobAssetRequestBuffer.Clear();
                        var current = uiObject.view[i];

                        var currentChildren = uiObject.view.GetChildren(current).ToArray();
                        DstEntityManager.AddComponent<UINode>(nodes[i]);
                        DstEntityManager.AddComponentData(nodes[i], new UIRoot(rootEntity));
                        DstEntityManager.AddComponentData<UIRenderQuadCount>(nodes[i], 1);
                        var material = uiObject.view.group.Material;
                        if (!string.IsNullOrWhiteSpace(current.name))
                        {
                            DstEntityManager.AddComponentData<UINodeName>(nodes[i], current.name);
                            DstEntityManager.SetName(nodes[i], $"{baseName} [Node {i}] #{current.name}");
                        }
                        else
                        {
                            DstEntityManager.SetName(nodes[i], $"{baseName} [Node {i}]");
                        }
                        var classBuffer = DstEntityManager.AddBuffer<UINodeClass>(nodes[i]);
                        for (int j = 0; j < current.classes.Length; j++)
                        {
                            classBuffer.Add(current.classes[j]);
                        }
                        var childBuffer = DstEntityManager.AddBuffer<UINodeChild>(nodes[i]);
                        for (int j = 0; j < currentChildren.Length; j++)
                        {
                            childBuffer.Add(nodes[uiObject.view.IndexOf(currentChildren[j])]);
                        }
                        for (int j = 0; j < currentChildren.Length; j++)
                        {
                            DstEntityManager.AddComponentData<UINodeParent>(nodes[uiObject.view.IndexOf(currentChildren[j])], nodes[i]);
                        }
                        DstEntityManager.AddComponentData(nodes[i], new LocalToWorld
                        {
                            Value = uiObject.gameObject.transform.localToWorldMatrix
                        });
                        DstEntityManager.AddComponentData(nodes[i], new RenderBounds { Value = uiObject.mesh.GetSubMesh(i).bounds.ToAABB() });
                        CreateNodeConfigBuffers(uiObject.view[i], i, nodes[i], uiObject.view.group, blobAssetRequestBuffer);
                        RenderMeshUtility.AddComponents(nodes[i], DstEntityManager, new RenderMeshDescription(
                            uiObject.mesh,
                            material,
                            UnityEngine.Rendering.ShadowCastingMode.Off,
                            false,
                            MotionVectorGenerationMode.Camera,
                            0,
                            i, 1));
                        DstEntityManager.AddComponentData<UISubmeshIndex>(nodes[i], i);

                        foreach (var bindingComponent in GetMaterialBindingComponents(uiObject.view[i], material))
                        {
                            DstEntityManager.AddComponent(nodes[i], bindingComponent);
                        }
                        if (blobAssetRequestBuffer.Count > 0 && rootBlobInfo != null)
                        {
                            var requestInfo = DstEntityManager.AddBuffer<UIBlobAssetPropertyRequestInfo>(nodes[i]);
                            for (int j = 0; j < blobAssetRequestBuffer.Count; j++)
                            {
                                var index = rootBlobInfo.IndexOf(blobAssetRequestBuffer[j].hash);
                                if (index >= 0)
                                {
                                    requestInfo.Add(new UIBlobAssetPropertyRequestInfo(index, blobAssetRequestBuffer[j].block, blobAssetRequestBuffer[j].offset));
                                }
                            }
                        }
                        if (modelInfo != null && modelInfo.properties.TryGetValue(uiObject.view[i].modelPropertyBinding, out var propertyInfo))
                        {
                            foreach (var component in propertyInfo.components)
                            {
                                DstEntityManager.AddComponent(nodes[i], component);
                                UIConversionComponentManager.InitializePropertyComponents(component, DstEntityManager, i, uiObject.view, nodes);
                            }
                        }
                    }
                    ConfigureEditorRenderData(nodes[0], uiObject.gameObject, true);
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        ConfigureEditorRenderData(nodes[i], uiObject.gameObject, true);
                    }
                    nodes.Dispose();
                    DeclareLinkedEntityGroup(uiObject.gameObject);
                }
            });
            Profiler.EndSample();
        }
        internal unsafe void CreateNodeConfigBuffers(UIViewAsset.Node node, int index, Entity entity, UIGroup group, List<BlobAssetRequest> blobRequests)
        {
            var extraDataStream = new MemoryBinaryWriter();
            var context = new ParserContext
            {
                group = group,
                extraDataStream = extraDataStream
            };
            var configHeader = new UIModelConfigHeader();
            UIRuntimeDataHeader runtimeData = default;
            var element = UIElementManager.GetElement(node.type);
            var blocks = UIEditorUtility.GetPropertyBlocks(element);
            var blockHeaders = stackalloc UIModelConfigPropertyBlockHeader[blocks.Count];
            DstEntityManager.AddComponent(entity, ComponentType.ReadWrite(typeof(UIElementTag<>).MakeGenericType(node.type)));
            configHeader.count = (byte)blocks.Count;
            configHeader.element = element;
            int configBufferSize = UnsafeUtility.SizeOf<UIRuntimeDataHeader>() + UnsafeUtility.SizeOf<UIModelConfigHeader>();
            var offsets = new Dictionary<Type, int>();
            for (int i = 0; i < blocks.Count; i++)
            {
                var size = UnsafeUtility.SizeOf(blocks[i].type);
                offsets[blocks[i].type] = configBufferSize;
                configBufferSize += UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>() + size;
                configHeader.length += size + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>();
                blockHeaders[i] = new UIModelConfigPropertyBlockHeader
                {
                    hash = blocks[i].hash,
                    length = (ushort)size,
                    enabled = (byte)(blocks[i].required ? 1 : 0)
                };
                DstEntityManager.AddComponent(entity, ComponentType.ReadWrite(typeof(UIPropertyBlockTag<>).MakeGenericType(blocks[i].type)));
            }
            var bufferData = DstEntityManager.AddBuffer<UIConfigBufferData>(entity);
            bufferData.EnsureCapacity(configBufferSize);
            bufferData.Length = configBufferSize;
            var configBuffer = (IntPtr)bufferData.GetUnsafePtr();
            UnsafeUtility.MemClear(configBuffer.ToPointer(), configBufferSize);
            UnsafeUtility.CopyStructureToPtr(ref runtimeData, configBuffer.ToPointer());
            UnsafeUtility.CopyStructureToPtr(ref configHeader, (configBuffer + UnsafeUtility.SizeOf<UIRuntimeDataHeader>()).ToPointer());
            //Block Headers
            for (int i = 0; i < blocks.Count; i++)
            {
                UnsafeUtility.CopyStructureToPtr(ref blockHeaders[i], (configBuffer + offsets[blocks[i].type]).ToPointer());
            }
            //Default Values
            for (int i = 0; i < blocks.Count; i++)
            {
                foreach (var field in blocks[i].type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var defaultValueAttrs = field.GetCustomAttributes<DefaultValueAttribute>().ToArray();
                    if (defaultValueAttrs == null || (defaultValueAttrs.Length > 1 && !typeof(IUIBool).IsAssignableFrom(field.FieldType)))
                        continue;
                    var fieldOffset = UnsafeUtility.GetFieldOffset(field);

                    foreach (var defaultValueAttr in defaultValueAttrs)
                    {
                        typeof(UIEditorUtility).GetMethod(nameof(UIEditorUtility.HandleProperty), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(field.FieldType).Invoke(null, new object[] { defaultValueAttr.value, configBuffer, offsets[blocks[i].type] + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>(), fieldOffset, defaultValueAttr.bitOffset, context });
                    }
                }
            }
            //Preprocess element
            if (UIECSProcessorManager.elementPreprocessors.TryGetValue(node.type, out var preprocessor))
            {
                preprocessor.Preprocess(configBuffer.ToPointer(), configBufferSize);
            }
            //Preprocess blocks
            for (int i = 0; i < blocks.Count; i++)
            {
                if (UIECSProcessorManager.propertyBlockPreprocessors.TryGetValue(blocks[i].type, out var blockPreprocessor))
                {
                    var ptr = (configBuffer + offsets[blocks[i].type]);
                    var processorContext = PropertyBlockProcessorContext.Create(group, index, blocks[i].hash, blobRequests);
                    blockPreprocessor.Preprocess((UIModelConfigPropertyBlockHeader*)ptr, (ptr + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>()).ToPointer(), configBufferSize, processorContext);
                }
            }
            //Set Values
            if (node.properties != null)
            {
                foreach (var property in node.properties)
                {
                    var blockProperty = UIElementManager.GetProperty(property.name);
                    var blockType = UIElementManager.GetPropertyBlock(blockProperty.blockHash);
                    context.property = blockProperty;
                    typeof(UIEditorUtility).GetMethod(nameof(UIEditorUtility.HandleProperty), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(UIElementManager.GetPropertyType(blockProperty)).Invoke(null, new object[] { property.value, configBuffer, offsets[blockType] + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>(), blockProperty.offset, blockProperty.bitOffset, context });
                }
            }
            //Postprocess blocks
            for (int i = 0; i < blocks.Count; i++)
            {
                if (UIECSProcessorManager.propertyBlockPostprocessors.TryGetValue(blocks[i].type, out var blockPostprocessor))
                {
                    var processorContext = PropertyBlockProcessorContext.Create(group, index, blocks[i].hash, blobRequests);
                    var ptr = (configBuffer + offsets[blocks[i].type]);

                    blockPostprocessor.Postprocess((UIModelConfigPropertyBlockHeader*)ptr, (ptr + UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>()).ToPointer(), configBufferSize, processorContext);
                }
            }
            //Postprocess element
            if (UIECSProcessorManager.elementPostprocessors.TryGetValue(node.type, out var postprocessor))
            {
                postprocessor.Postprocess(configBuffer.ToPointer(), configBufferSize);
            }
            var extraBufferData = DstEntityManager.AddBuffer<UIConfigBufferExtraData>(entity);
            if (extraDataStream.Length > 0)
            {
                extraBufferData.EnsureCapacity(configBufferSize + extraDataStream.Length);
                extraBufferData.Length = configBufferSize + extraDataStream.Length;
                UnsafeUtility.MemCpy(extraBufferData.GetUnsafePtr(), extraDataStream.Data, extraDataStream.Length);
            }
            extraDataStream.Dispose();
            var extraDataTypeAttribute = node.type.Value.GetCustomAttribute<UIExtraDataTypeAttribute>();
            if (extraDataTypeAttribute != null)
            {
                DstEntityManager.AddComponent(entity, ComponentType.ReadOnly(typeof(UIConfigBufferExtraDataType<>).MakeGenericType(extraDataTypeAttribute.type)));
            }
        }
        internal unsafe static ComponentType[] GetMaterialBindingComponents(UIViewAsset.Node node, Material material) => GetMaterialBindingComponents(UIElementManager.GetElement(node.type), material);
        internal unsafe static ComponentType[] GetMaterialBindingComponents(ulong element, Material material)
        {
            var blocks = UIEditorUtility.GetPropertyBlocks(element);
            var bindingComponents = new List<ComponentType>();
            foreach (var block in blocks)
            {
                if (UIConversionComponentManager.TryGetBindingComponents(block.hash, out var blockBindingComponents))
                {
                    bindingComponents.AddRange(blockBindingComponents.Where(t => material.HasProperty(t)).Select(t => UIConversionComponentManager.GetBindingComponent(t)));
                }
            }
            return bindingComponents.Distinct().ToArray();
        }
    }

    public struct UIObjectNodeConversionData_ : IBufferElementData
    {
        public Entity destinationEntity;
        public int index;
    }
}
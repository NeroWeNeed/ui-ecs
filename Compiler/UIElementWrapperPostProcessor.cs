using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using NeroWeNeed.Commons.AssemblyAnalyzers.Editor;
using Unity.Burst;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace NeroWeNeed.UIECS.Compiler
{
    public class UIElementWrapperPostProcessor : ILPostProcessor
    {
        private CecilPostProcessor postProcessor = new CecilPostProcessor();

        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            return postProcessor.Process(compiledAssembly);
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return postProcessor.WillProcess(compiledAssembly);
        }
        public class CecilPostProcessor : UIElementCecilPostProcessor
        {
            private static readonly (Type, Type)[] uiElementInterfaces = new (Type, Type)[] {
            (typeof(IUIElementLayout),typeof(Layout)),
            (typeof(IUIElementConstrain),typeof(Constrain)),
            (typeof(IUIElementSize),typeof(Size)),
            (typeof(IUIElementGenerateMeshData),typeof(GenerateMeshData))
        };


            public override bool Process(AssemblyDefinition assemblyDefinition, List<DiagnosticMessage> diagnostics)
            {
                var wrapperTypeCache = new Dictionary<TypeDefinition, CacheData>();
                var interfaceTypeDefinitions = uiElementInterfaces.Select(elementInterface => new InterfaceData
                {
                    interfaceTypeDefinition = assemblyDefinition.MainModule.ImportReference(elementInterface.Item1).Resolve(),
                    delegateTypeReference = assemblyDefinition.MainModule.ImportReference(elementInterface.Item2)
                });
                var changed = false;
                var genericComponents = new List<CustomAttribute>();
                foreach (var moduleDefinition in assemblyDefinition.Modules)
                {
                    foreach (var typeDefinition in moduleDefinition.Types)
                    {
                        if (!typeDefinition.HasGenericParameters && typeDefinition.IsValueType && typeDefinition.HasInterface<IUINode>())
                        {
                            foreach (var uiElementInterface in interfaceTypeDefinitions)
                            {
                                if (typeDefinition.HasInterface(uiElementInterface.interfaceTypeDefinition))
                                {
                                    if (!wrapperTypeCache.TryGetValue(typeDefinition, out CacheData cacheData))
                                    {
                                        var wrapperType = new TypeDefinition(typeDefinition.Namespace, $"{typeDefinition.Name}$UIWrapper", Mono.Cecil.TypeAttributes.Sealed | Mono.Cecil.TypeAttributes.Abstract, moduleDefinition.TypeSystem.Object);
                                        wrapperType.CustomAttributes.Add(new CustomAttribute(moduleDefinition.ImportReference(typeof(BurstCompileAttribute).GetConstructor(Type.EmptyTypes))));
                                        cacheData = new CacheData
                                        {
                                            wrapperTypeDefinition = wrapperType,
                                            initHandlers = CollectInitializableFields(typeDefinition)
                                        };
                                        wrapperTypeCache[typeDefinition] = cacheData;
                                    }
                                    CreateWrapperType(assemblyDefinition, moduleDefinition, typeDefinition, cacheData.wrapperTypeDefinition, cacheData.initHandlers, uiElementInterface.interfaceTypeDefinition, uiElementInterface.delegateTypeReference);
                                    changed = true;
                                }
                            }
                            if (typeDefinition.HasAttribute<UIExtraDataTypeAttribute>())
                            {
                                var extraDataType = typeDefinition.GetAttribute<UIExtraDataTypeAttribute>().ConstructorArguments[0];
                                var genericComponentAttr = new CustomAttribute(moduleDefinition.ImportReference(typeof(RegisterGenericComponentTypeAttribute).GetConstructor(new Type[] { typeof(Type) })));
                                var t = new GenericInstanceType(moduleDefinition.ImportReference(typeof(UIConfigBufferExtraDataType<>)));
                                t.GenericArguments.Add((TypeReference)extraDataType.Value);
                                genericComponentAttr.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.TypedReference, moduleDefinition.ImportReference(t)));
                                genericComponents.Add(genericComponentAttr);
                                changed = true;
                            }
                        }
                    }
                    foreach (var cacheData in wrapperTypeCache.Values)
                    {
                        moduleDefinition.Types.Add(cacheData.wrapperTypeDefinition);
                    }
                    wrapperTypeCache.Clear();
                }
                foreach (var genericComponent in genericComponents)
                {
                    assemblyDefinition.CustomAttributes.Add(genericComponent);
                }
                return changed;
            }
            private TypeDefinition CreateWrapperType(AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition, TypeDefinition uiTypeDefinition, TypeDefinition wrapperType, List<IFieldInitHandler> fieldInitHandlers, TypeDefinition interfaceType, TypeReference delegateTypeReference)
            {

                var interfaceMethodDefinition = interfaceType.Methods[0];
                var sourceMethodDefinition = uiTypeDefinition.Methods.First(method => method.IsHideBySig && method.Name == interfaceMethodDefinition.Name);
                var wrapperMethodDefinition = new MethodDefinition(sourceMethodDefinition.Name, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, sourceMethodDefinition.ReturnType);
                foreach (var parameter in sourceMethodDefinition.Parameters)
                {
                    wrapperMethodDefinition.Parameters.Add(parameter);
                }

                var elementVariable = new VariableDefinition(uiTypeDefinition);
                wrapperMethodDefinition.Body.Variables.Add(elementVariable);
                wrapperMethodDefinition.CustomAttributes.Add(new CustomAttribute(moduleDefinition.ImportReference(typeof(BurstCompileAttribute).GetConstructor(Type.EmptyTypes))));
                var pInvokeCallback = new CustomAttribute(moduleDefinition.ImportReference(typeof(MonoPInvokeCallbackAttribute).GetConstructor(new Type[] { typeof(Type) })));
                pInvokeCallback.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.TypedReference, delegateTypeReference));
                wrapperMethodDefinition.CustomAttributes.Add(pInvokeCallback);
                wrapperMethodDefinition.Body.InitLocals = true;
                wrapperMethodDefinition.Body.SimplifyMacros();
                var processor = wrapperMethodDefinition.Body.GetILProcessor();
                processor.Emit(OpCodes.Ldloca, elementVariable);
                processor.Emit(OpCodes.Initobj, uiTypeDefinition);
                foreach (var fieldInitHandler in fieldInitHandlers)
                {
                    processor.Emit(OpCodes.Ldloca, elementVariable);
                    fieldInitHandler.Emit(processor, assemblyDefinition, moduleDefinition, uiTypeDefinition, wrapperType);
                    processor.Emit(OpCodes.Stfld, fieldInitHandler.Target);
                }
                processor.Emit(OpCodes.Ldloca, elementVariable);
                foreach (var parameter in wrapperMethodDefinition.Parameters)
                {
                    processor.Emit(OpCodes.Ldarg, parameter);
                }
                processor.Emit(OpCodes.Call, sourceMethodDefinition);
                processor.Emit(OpCodes.Ret);
                wrapperMethodDefinition.Body.OptimizeMacros();
                wrapperType.Methods.Add(wrapperMethodDefinition);

                return wrapperType;
            }

            private List<IFieldInitHandler> CollectInitializableFields(TypeDefinition uiTypeDefinition)
            {
                var fieldInitHandlers = new List<IFieldInitHandler>();
                foreach (var field in uiTypeDefinition.Fields)
                {
                    if (field.FieldType.IsGenericInstance && field.FieldType.FullName.StartsWith("NeroWeNeed.UIECS.PropertyBlockAccessor"))
                    {
                        fieldInitHandlers.Add(new PropertyBlockFieldInitHandler { Target = field, PropertyBlock = ((GenericInstanceType)field.FieldType).GenericArguments[0] });
                    }
                    else if (field.FieldType.IsGenericInstance && field.FieldType.FullName.StartsWith("NeroWeNeed.UIECS.OptionalPropertyBlockAccessor"))
                    {
                        fieldInitHandlers.Add(new OptionalPropertyBlockFieldInitHandler { Target = field, PropertyBlock = ((GenericInstanceType)field.FieldType).GenericArguments[0] });
                    }
                    else if (field.FieldType.FullName == typeof(SizeAccessor).FullName)
                    {
                        fieldInitHandlers.Add(new SizeFieldInitHandler { Target = field });
                    }
                    else if (field.FieldType.FullName == typeof(PositionAccessor).FullName)
                    {
                        fieldInitHandlers.Add(new PositionFieldInitHandler { Target = field });
                    }
                    else if (field.FieldType.FullName == typeof(ConstraintAccessor).FullName)
                    {
                        fieldInitHandlers.Add(new ConstraintsFieldInitHandler { Target = field });
                    }
                    /*                     else if (field.FieldType.FullName == typeof(ExtraDataAccessor).FullName) {
                                            fieldInitHandlers.Add(new ExtraDataFieldInitHandler { Target = field });
                                        } */
                }
                return fieldInitHandlers;
            }

            internal interface IFieldInitHandler
            {
                public FieldDefinition Target { get; set; }
                public void Emit(ILProcessor processor, AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition, TypeDefinition uiTypeDefinition, TypeDefinition wrapperTypeDefinition);
            }

            internal struct BindToMaterialProperty
            {
                public TypeReference type;
                public MaterialPropertyFormat format;

            }
            internal struct PropertyBlockFieldInitHandler : IFieldInitHandler
            {
                public FieldDefinition Target { get; set; }
                public TypeReference PropertyBlock { get; set; }

                public void Emit(ILProcessor processor, AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition, TypeDefinition uiTypeDefinition, TypeDefinition wrapperTypeDefinition)
                {
                    //int offset = GetPropertyBlockOffset(uiTypeDefinition.FullName, PropertyBlock.FullName) + Marshal.SizeOf<UIModelConfigPropertyBlockHeader>();
                    var offsetCall = new GenericInstanceMethod(moduleDefinition.ImportReference(typeof(UIConfigHandleExtensions).GetMethod(nameof(UIConfigHandleExtensions.GetPropertyBlock))));
                    offsetCall.GenericArguments.Add(PropertyBlock);
                    processor.Emit(OpCodes.Ldarg_0);
                    //processor.Emit(OpCodes.Ldfld, moduleDefinition.ImportReference(typeof(UIConfigHandle).GetField(nameof(UIConfigHandle.value))));
                    processor.Emit(OpCodes.Call, offsetCall);
                    var accessorType = (GenericInstanceType)Target.FieldType;
                    var ctor = moduleDefinition.ImportReference(accessorType.Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 1)).MakeHostInstanceGeneric(PropertyBlock);
                    processor.Emit(OpCodes.Newobj, ctor);
                }
            }
            /*             internal struct ExtraDataFieldInitHandler : IFieldInitHandler
                        {
                            public FieldDefinition Target { get; set; }

                            public void Emit(ILProcessor processor, AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition, TypeDefinition uiTypeDefinition, TypeDefinition wrapperTypeDefinition)
                            {
                                processor.Emit(OpCodes.Ldarg_0);
                                processor.Emit(OpCodes.Call, offsetCall);
                                processor.Emit(OpCodes.Stloc, headerVariable);
                                processor.Emit(OpCodes.Ldloc, headerVariable);
                                processor.Emit(OpCodes.Ldc_I4, sizeof(UIModelConfigPropertyBlockHeader));
                                processor.Emit(OpCodes.Add);
                                processor.Emit(OpCodes.Ldloc, headerVariable);
                                processor.Emit(OpCodes.Ldc_I4, ((int)Marshal.OffsetOf<UIModelConfigPropertyBlockHeader>(nameof(UIModelConfigPropertyBlockHeader.enabled))));
                                processor.Emit(OpCodes.Add);
                            }
                        } */
            internal unsafe struct OptionalPropertyBlockFieldInitHandler : IFieldInitHandler
            {
                public FieldDefinition Target { get; set; }
                public TypeReference PropertyBlock { get; set; }
                public void Emit(ILProcessor processor, AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition, TypeDefinition uiTypeDefinition, TypeDefinition wrapperTypeDefinition)
                {
                    var offsetCall = new GenericInstanceMethod(moduleDefinition.ImportReference(typeof(UIConfigHandleExtensions).GetMethod(nameof(UIConfigHandleExtensions.GetPropertyBlockHeader))));
                    offsetCall.GenericArguments.Add(PropertyBlock);
                    var headerVariable = new VariableDefinition(moduleDefinition.TypeSystem.IntPtr);
                    processor.Body.Variables.Add(headerVariable);
                    processor.Emit(OpCodes.Ldarg_0);
                    processor.Emit(OpCodes.Call, offsetCall);
                    processor.Emit(OpCodes.Stloc, headerVariable);
                    processor.Emit(OpCodes.Ldloc, headerVariable);
                    processor.Emit(OpCodes.Ldc_I4, sizeof(UIModelConfigPropertyBlockHeader));
                    processor.Emit(OpCodes.Add);
                    processor.Emit(OpCodes.Ldloc, headerVariable);
                    processor.Emit(OpCodes.Ldc_I4, ((int)Marshal.OffsetOf<UIModelConfigPropertyBlockHeader>(nameof(UIModelConfigPropertyBlockHeader.enabled))));
                    processor.Emit(OpCodes.Add);
                    /*                 //processor.Emit(OpCodes.Ldfld, moduleDefinition.ImportReference(typeof(UIConfigHandle).GetField(nameof(UIConfigHandle.value))));
                                    if (ptrOffset > 0) {
                                        processor.Emit(OpCodes.Ldc_I4, ptrOffset);
                                        processor.Emit(OpCodes.Add);
                                    }

                                    processor.Emit(OpCodes.Ldarg_0);
                                    processor.Emit(OpCodes.Ldfld, moduleDefinition.ImportReference(typeof(UIConfigHandle).GetField(nameof(UIConfigHandle.value))));
                                    if (flagOffset > 0) {
                                        processor.Emit(OpCodes.Ldc_I4, flagOffset);
                                        processor.Emit(OpCodes.Add);
                                    } */

                    var accessorType = (GenericInstanceType)Target.FieldType;
                    var ctor = moduleDefinition.ImportReference(accessorType.Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 2)).MakeHostInstanceGeneric(PropertyBlock);
                    processor.Emit(OpCodes.Newobj, ctor);
                }
            }
            internal struct SizeFieldInitHandler : IFieldInitHandler
            {
                public FieldDefinition Target { get; set; }
                public void Emit(ILProcessor processor, AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition, TypeDefinition uiTypeDefinition, TypeDefinition wrapperTypeDefinition)
                {
                    processor.Emit(OpCodes.Ldarg_0);
                    processor.Emit(OpCodes.Ldfld, moduleDefinition.ImportReference(typeof(UIConfigHandle).GetField(nameof(UIConfigHandle.value))));
                    var offset = (int)Marshal.OffsetOf<UIRuntimeDataHeader>(nameof(UIRuntimeDataHeader.size));
                    if (offset > 0)
                    {
                        processor.Emit(OpCodes.Ldc_I4, offset);
                        processor.Emit(OpCodes.Add);
                    }

                    var accessorType = Target.FieldType;
                    var ctor = moduleDefinition.ImportReference(accessorType.Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 1));
                    processor.Emit(OpCodes.Newobj, ctor);
                }
            }
            internal struct ConstraintsFieldInitHandler : IFieldInitHandler
            {
                public FieldDefinition Target { get; set; }
                public void Emit(ILProcessor processor, AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition, TypeDefinition uiTypeDefinition, TypeDefinition wrapperTypeDefinition)
                {
                    processor.Emit(OpCodes.Ldarg_0);
                    processor.Emit(OpCodes.Ldfld, moduleDefinition.ImportReference(typeof(UIConfigHandle).GetField(nameof(UIConfigHandle.value))));
                    //var offset = UnsafeUtility.GetFieldOffset(typeof(UIRuntimeDataHeader).GetField(nameof(UIRuntimeDataHeader.constraints)));
                    var offset = (int)Marshal.OffsetOf<UIRuntimeDataHeader>(nameof(UIRuntimeDataHeader.constraints));
                    if (offset > 0)
                    {
                        processor.Emit(OpCodes.Ldc_I4, offset);
                        processor.Emit(OpCodes.Add);
                    }
                    var accessorType = Target.FieldType;
                    var ctor = moduleDefinition.ImportReference(accessorType.Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 1));
                    processor.Emit(OpCodes.Newobj, ctor);
                }

            }
            internal struct PositionFieldInitHandler : IFieldInitHandler
            {
                public FieldDefinition Target { get; set; }
                public void Emit(ILProcessor processor, AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition, TypeDefinition uiTypeDefinition, TypeDefinition wrapperTypeDefinition)
                {
                    processor.Emit(OpCodes.Ldarg_0);
                    processor.Emit(OpCodes.Ldfld, moduleDefinition.ImportReference(typeof(UIConfigHandle).GetField(nameof(UIConfigHandle.value))));
                    //var offset = UnsafeUtility.GetFieldOffset(typeof(UIRuntimeDataHeader).GetField(nameof(UIRuntimeDataHeader.position)));
                    var offset = (int)Marshal.OffsetOf<UIRuntimeDataHeader>(nameof(UIRuntimeDataHeader.position));
                    if (offset > 0)
                    {
                        processor.Emit(OpCodes.Ldc_I4, offset);
                        processor.Emit(OpCodes.Add);
                    }
                    var accessorType = Target.FieldType;
                    var ctor = moduleDefinition.ImportReference(accessorType.Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 1));
                    processor.Emit(OpCodes.Newobj, ctor);
                }
            }
            private struct CacheData
            {
                public TypeDefinition wrapperTypeDefinition;
                public List<IFieldInitHandler> initHandlers;
            }
            private struct InterfaceData
            {
                public TypeDefinition interfaceTypeDefinition;
                public TypeReference delegateTypeReference;
            }
            public struct PropertyBlockInfo
            {
                public Type type;
                public bool required;
                public ulong hash;

                public PropertyBlockInfo(Type type, bool required, ulong hash)
                {
                    this.type = type;
                    this.required = required;
                    this.hash = hash;
                }
            }
        }
    }

}
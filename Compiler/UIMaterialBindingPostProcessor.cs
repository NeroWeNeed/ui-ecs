using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using NeroWeNeed.Commons.AssemblyAnalyzers.Editor;
using NeroWeNeed.Commons.Editor;
using NeroWeNeed.UIECS.Jobs;
using NeroWeNeed.UIECS.Systems;
using Unity.Burst;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

namespace NeroWeNeed.UIECS.Compiler
{
    public class UIMaterialBindingPostProcessor : ILPostProcessor
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
            internal unsafe static readonly Dictionary<string, ConversionInfo> conversions = new Dictionary<string, ConversionInfo>()
            {
                { typeof(byte).FullName, new ConversionInfo(TypeCode.Byte,sizeof(byte),1)},
                { typeof(ushort).FullName, new ConversionInfo(TypeCode.UInt16,sizeof(ushort),1)},
                { typeof(uint).FullName, new ConversionInfo(TypeCode.UInt32,sizeof(uint),1)},
                { typeof(ulong).FullName, new ConversionInfo(TypeCode.UInt64,sizeof(ulong),1)},
                { typeof(sbyte).FullName, new ConversionInfo(TypeCode.SByte,sizeof(sbyte),1)},
                { typeof(short).FullName, new ConversionInfo(TypeCode.Int16,sizeof(short),1)},
                { typeof(int).FullName, new ConversionInfo(TypeCode.Int32,sizeof(int),1)},
                { typeof(long).FullName, new ConversionInfo(TypeCode.Int64,sizeof(long),1)},
                { typeof(float).FullName, new ConversionInfo(TypeCode.Single,sizeof(float),1)},
                { typeof(double).FullName, new ConversionInfo(TypeCode.Double,sizeof(double),1)},
                { typeof(UILength).FullName, new ConversionInfo(TypeCode.Single,sizeof(UILength),1)},
                { typeof(UnityEngine.Color).FullName, new ConversionInfo(TypeCode.Single,sizeof(UnityEngine.Color),4)},
                { typeof(UnityEngine.Color32).FullName, new ConversionInfo(TypeCode.Byte,sizeof(UnityEngine.Color32),4)}
            };
            public override bool Process(AssemblyDefinition assemblyDefinition, List<DiagnosticMessage> diagnostics)
            {
                var newTypes = new List<TypeDefinition>();
                var bindingComponents = new List<UIBindingComponentType>();
                bool changed = false;
                foreach (var moduleDefinition in assemblyDefinition.Modules)
                {
                    foreach (var typeDefinition in moduleDefinition.Types)
                    {
                        if (typeDefinition.HasInterface<IPropertyBlock>())
                        {
                            foreach (var field in typeDefinition.Fields)
                            {
                                if (field.HasAttribute<UIMaterialPropertyAttribute>())
                                {
                                    var attr = field.GetAttribute<UIMaterialPropertyAttribute>();
                                    NormalizerHint[] attrNormalizerHints = attr.GetArgumentArray<NormalizerHint>(0);
                                    bool attrAutoGen = attr.GetProperty<bool>(nameof(UIMaterialPropertyAttribute.AutoGenerate), true);
                                    if (attrAutoGen)
                                    {
                                        var name = field.HasAttribute<PropertyAttribute>() ? (string)field.GetAttribute<PropertyAttribute>().ConstructorArguments[0].Value : field.Name;
                                        var blockName = field.DeclaringType.HasAttribute<PropertyBlockAttribute>() ? (string)field.DeclaringType.GetAttribute<PropertyBlockAttribute>().ConstructorArguments[0].Value : null;
                                        foreach (var componentType in UIBindingComponentType.TryCreate(moduleDefinition, field, blockName, name, diagnostics))
                                        {

                                            newTypes.Add(componentType.typeDefinition);
                                            var systemType = new UIBindingUnmanagedSystemType(moduleDefinition, componentType, attrNormalizerHints, diagnostics);
                                            newTypes.Add(systemType.definition);
                                            bindingComponents.Add(componentType);
                                            changed = true;
                                        }

                                    }
                                }
                            }
                        }
                    }
                    foreach (var newType in newTypes)
                    {
                        moduleDefinition.Types.Add(newType);
                    }
                    newTypes.Clear();
                }
                foreach (var genericJobAttribute in bindingComponents.Select(b => b.CreateRegisterGenericJobAttribute(assemblyDefinition.MainModule)))
                {
                    assemblyDefinition.CustomAttributes.Add(genericJobAttribute);
                }
                return changed;
            }

            internal static bool TryGetConversion(TypeReference typeReference, ModuleDefinition moduleDefinition, List<DiagnosticMessage> messages, out ConversionComponentInfo componentInfo)
            {
                var resolvedTypeReference = typeReference.Resolve();
                if (typeReference.IsGenericInstance && resolvedTypeReference.HasInterface<ICompositeData>())
                {
                    var genericTypeReference = ((GenericInstanceType)typeReference);
                    var arg = genericTypeReference.GenericArguments[0];
                    if (TryGetConversion(arg, moduleDefinition, messages, out var nestedCompositeInfo))
                    {
                        int count = 0;
                        if (resolvedTypeReference.HasInterface<ICompositeData4>())
                        {
                            count = 4;
                        }
                        else if (resolvedTypeReference.HasInterface<ICompositeData3>())
                        {
                            count = 3;
                        }
                        else if (resolvedTypeReference.HasInterface<ICompositeData2>())
                        {
                            count = 2;
                        }
                        var compositeInfo = nestedCompositeInfo.conversionInfo.GetCompositeConversionInfo(count);
                        if (compositeInfo.IsValid)
                        {
                            componentInfo = new ConversionComponentInfo
                            {
                                elementType = arg,
                                conversionInfo = compositeInfo,
                                count = 1
                            };
                            return true;
                        }
                        else
                        {
                            componentInfo = new ConversionComponentInfo
                            {
                                elementType = arg,
                                conversionInfo = nestedCompositeInfo.conversionInfo,
                                count = count
                            };
                            return true;
                        }
                    }
                }
                if (conversions.TryGetValue(resolvedTypeReference.FullName, out var result))
                {
                    componentInfo = new ConversionComponentInfo
                    {
                        elementType = typeReference,
                        conversionInfo = result,
                        count = 1
                    };
                    return true;
                }
                else
                {
                    componentInfo = default;
                    return false;
                }
            }

            internal class UIBindingComponentType
            {
                public TypeDefinition typeDefinition;
                public FieldDefinition valueFieldDefinition;
                public TypeReference propertyTypeReference;
                public TypeReference propertyElementTypeReference;
                public string propertyName;
                public string materialPropertyName;
                public string subName;
                public TypeCode sourceTypeCode;
                public int fieldOffset;
                public int elementCount;
                public static UIBindingComponentType[] TryCreate(ModuleDefinition moduleDefinition, FieldDefinition propertyFieldDefinition, string blockName, string name, List<DiagnosticMessage> messages)
                {
                    if (TryGetConversion(propertyFieldDefinition.FieldType, moduleDefinition, messages, out var info))
                    {
                        var result = new UIBindingComponentType[info.count];
                        if (info.count == 1)
                        {
                            var propertyName = UIElementNameUtility.CreatePropertyName(blockName, name);
                            result[0] = new UIBindingComponentType(moduleDefinition, propertyFieldDefinition, UIElementNameUtility.ToMaterialName(propertyName), propertyName, info, 0, null);
                        }
                        else
                        {

                            var compositeNameAttribute = propertyFieldDefinition.GetAttribute<CompositeNameAttribute>();
                            var subNames = new string[4] {
                                                            compositeNameAttribute?.GetArgument<string>(0) ?? "x",
                                                            compositeNameAttribute?.GetArgument<string>(1) ?? "y",
                                                            compositeNameAttribute?.GetArgument<string>(2) ?? "z",
                                                            compositeNameAttribute?.GetArgument<string>(3) ?? "w"
                                                        };
                            var prefix = compositeNameAttribute.GetProperty(nameof(CompositeNameAttribute.Prefix), true);
                            for (int i = 0; i < info.count; i++)
                            {
                                var propertyName = UIElementNameUtility.CreatePropertyName(blockName, subNames[i], name, prefix);
                                result[i] = new UIBindingComponentType(moduleDefinition, propertyFieldDefinition, UIElementNameUtility.ToMaterialName(propertyName), propertyName, info, 0, subNames[i]);

                            }


                            //return Array.Empty<UIBindingComponentType>();
                        }

                        return result;
                    }
                    else
                    {

                        messages.Add(new DiagnosticMessage
                        {
                            MessageData = $"Unable to create binding for {UIElementNameUtility.CreatePropertyName(blockName,name)}."
                        });
                        return Array.Empty<UIBindingComponentType>();


                    }
                }
                public UIBindingComponentType(
                    ModuleDefinition moduleDefinition,
                    FieldDefinition propertyFieldDefinition,
                    string materialPropertyName,
                    string propertyName,
                    ConversionComponentInfo info,
                    int offset,
                    string subName)
                {


                    typeDefinition = new TypeDefinition(propertyFieldDefinition.DeclaringType.Namespace, $"UIMaterialBinding{materialPropertyName}", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed | Mono.Cecil.TypeAttributes.SequentialLayout | Mono.Cecil.TypeAttributes.Serializable, moduleDefinition.ImportReference(typeof(ValueType)));
                    var materialPropertyAttr = new CustomAttribute(moduleDefinition.ImportReference(typeof(MaterialPropertyAttribute).GetConstructor(new Type[] { typeof(string), typeof(MaterialPropertyFormat), typeof(short) })));
                    materialPropertyAttr.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, materialPropertyName));
                    materialPropertyAttr.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.ImportReference(typeof(MaterialPropertyFormat)), info.conversionInfo.propertyFormat));
                    materialPropertyAttr.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.Int16, (short)-1));
                    typeDefinition.CustomAttributes.Add(materialPropertyAttr);
                    var uiMaterialPropertyAttr = new CustomAttribute(moduleDefinition.ImportReference(typeof(UIMaterialPropertyComponentAttribute).GetConstructor(new Type[] { typeof(string) })));
                    uiMaterialPropertyAttr.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, propertyName));
                    typeDefinition.CustomAttributes.Add(uiMaterialPropertyAttr);
                    valueFieldDefinition = new FieldDefinition("value", Mono.Cecil.FieldAttributes.Public, moduleDefinition.ImportReference(info.conversionInfo.fieldType));
                    typeDefinition.Fields.Add(valueFieldDefinition);
                    //Interface
                    typeDefinition.Interfaces.Add(new InterfaceImplementation(moduleDefinition.ImportReference(typeof(IUIMaterialProperty))));


                    this.propertyTypeReference = propertyFieldDefinition.FieldType;
                    this.materialPropertyName = materialPropertyName;
                    this.sourceTypeCode = info.conversionInfo.elementTypeCode;
                    this.fieldOffset = info.conversionInfo.offset + offset;
                    this.elementCount = info.conversionInfo.elementInputCount;
                    this.propertyElementTypeReference = info.elementType;
                    this.subName = subName;
                    this.propertyName = propertyName;
                }



                public CustomAttribute CreateRegisterGenericJobAttribute(ModuleDefinition moduleDefinition)
                {
                    var type = new GenericInstanceType(moduleDefinition.ImportReference(typeof(UIBindingJob<,>)));
                    type.GenericArguments.Add(typeDefinition);

                    type.GenericArguments.Add(propertyElementTypeReference);
                    var typeRef = moduleDefinition.ImportReference(type);
                    var attr = new CustomAttribute(moduleDefinition.ImportReference(typeof(RegisterGenericJobTypeAttribute).GetConstructor(new Type[] { typeof(Type) })));
                    attr.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.TypedReference, typeRef));
                    return attr;
                }

            }
            internal class UIBindingUnmanagedSystemType
            {
                public TypeDefinition definition;
                public UIBindingComponentType componentType;
                public TypeReference propertyTypeReference;
                public FieldDefinition queryFieldDefinition;
                public FieldDefinition hintFieldDefinition;
                public FieldDefinition propertyFieldDefinition;
                public MethodDefinition onCreateMethodDefinition;
                public MethodDefinition onUpdateMethodDefinition;
                public MethodDefinition onDestroyMethodDefinition;
                public UIBindingUnmanagedSystemType(ModuleDefinition moduleDefinition, UIBindingComponentType componentType,  NormalizerHint[] hints, List<DiagnosticMessage> messages)
                {
                    definition = new TypeDefinition(componentType.typeDefinition.Namespace, $"UIBindingSystem{componentType.materialPropertyName}", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed | Mono.Cecil.TypeAttributes.Serializable | Mono.Cecil.TypeAttributes.SequentialLayout, moduleDefinition.ImportReference(typeof(ValueType)));
                    definition.CustomAttributes.Add(new CustomAttribute(moduleDefinition.ImportReference(typeof(BurstCompileAttribute).GetConstructor(Type.EmptyTypes))));
                    var updateInGroupAttr = new CustomAttribute(moduleDefinition.ImportReference(typeof(UpdateInGroupAttribute).GetConstructor(new Type[] { typeof(Type) })));
                    updateInGroupAttr.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.TypedReference, moduleDefinition.ImportReference(typeof(UIBindMaterialPropertyGroup))));
                    definition.CustomAttributes.Add(updateInGroupAttr);
                    var worldSystemFilterAttr = new CustomAttribute(moduleDefinition.ImportReference(typeof(WorldSystemFilterAttribute).GetConstructor(new Type[] { typeof(WorldSystemFilterFlags) })));
                    const WorldSystemFilterFlags flags = WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default;
                    worldSystemFilterAttr.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.ImportReference(typeof(WorldSystemFilterFlags)), flags));
                    definition.CustomAttributes.Add(worldSystemFilterAttr);
                    definition.Interfaces.Add(new InterfaceImplementation(moduleDefinition.ImportReference(typeof(ISystemBase))));
                    this.componentType = componentType;
                    propertyTypeReference = componentType.propertyTypeReference;
                    queryFieldDefinition = new FieldDefinition("query", Mono.Cecil.FieldAttributes.Private, moduleDefinition.ImportReference(typeof(EntityQuery)));
                    definition.Fields.Add(queryFieldDefinition);
                    propertyFieldDefinition = new FieldDefinition("property", Mono.Cecil.FieldAttributes.Private, moduleDefinition.ImportReference(typeof(UIProperty)));
                    definition.Fields.Add(propertyFieldDefinition);
                    hintFieldDefinition = new FieldDefinition("hint", Mono.Cecil.FieldAttributes.Private, moduleDefinition.ImportReference(typeof(BindingHintData)));
                    definition.Fields.Add(hintFieldDefinition);
                    GenerateOnCreate(moduleDefinition, hints);
                    GenerateOnUpdate(moduleDefinition);
                    GenerateOnDestroy(moduleDefinition);
                }
                private void GenerateOnCreate(ModuleDefinition moduleDefinition, NormalizerHint[] hints)
                {
                    onCreateMethodDefinition = new MethodDefinition("OnCreate", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.ReuseSlot | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.Virtual, moduleDefinition.TypeSystem.Void);
                    var systemStateParameter = new ParameterDefinition("systemState", Mono.Cecil.ParameterAttributes.None, new ByReferenceType(moduleDefinition.ImportReference(typeof(SystemState))));
                    onCreateMethodDefinition.Parameters.Add(systemStateParameter);
                    onCreateMethodDefinition.Body.InitLocals = true;
                    onCreateMethodDefinition.Body.SimplifyMacros();
                    var processor = onCreateMethodDefinition.Body.GetILProcessor();
                    //References
                    var componentTypeReference = moduleDefinition.ImportReference(typeof(ComponentType));
                    //Init Query
                    processor.Emit(OpCodes.Ldarg_0);
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldc_I4_2);
                    processor.Emit(OpCodes.Newarr, componentTypeReference);
                    processor.Emit(OpCodes.Dup);
                    processor.Emit(OpCodes.Ldc_I4_0);
                    processor.Emit(OpCodes.Call, moduleDefinition.ImportReference(typeof(ComponentType).GetGenericMethod(nameof(ComponentType.ReadOnly), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).MakeGenericMethod(typeof(UIConfigBufferData))));
                    processor.Emit(OpCodes.Stelem_Any, componentTypeReference);
                    processor.Emit(OpCodes.Dup);
                    processor.Emit(OpCodes.Ldc_I4_1);
                    var m = new GenericInstanceMethod(moduleDefinition.ImportReference(typeof(ComponentType).GetGenericMethod(nameof(ComponentType.ReadWrite), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)));
                    m.GenericArguments.Add(componentType.typeDefinition);
                    processor.Emit(OpCodes.Call, m);
                    processor.Emit(OpCodes.Stelem_Any, componentTypeReference);
                    processor.Emit(OpCodes.Call, moduleDefinition.ImportReference(typeof(SystemState).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).First(m => m.Name == nameof(SystemState.GetEntityQuery) && m.GetParameters().FirstOrDefault()?.GetCustomAttribute<ParamArrayAttribute>() != null)));
                    processor.Emit(OpCodes.Stfld, queryFieldDefinition);
                    //Init Property
                    processor.Emit(OpCodes.Ldarg_0);
                    processor.Emit(OpCodes.Ldstr, componentType.propertyName);
                    processor.Emit(OpCodes.Call, moduleDefinition.ImportReference(typeof(UIElementManager).GetMethod(nameof(UIElementManager.GetProperty))));
                    processor.Emit(OpCodes.Stfld, propertyFieldDefinition);
                    //Init Hint
                    processor.Emit(OpCodes.Ldarg_0);
                    int currentHint = 0;
                    if (hints != null)
                    {
                        while (currentHint < hints.Length && currentHint < BindingHintData.HintSize)
                        {
                            processor.Emit(OpCodes.Ldc_I4, (int)hints[currentHint]);
                            currentHint++;
                        }
                    }
                    while (currentHint < BindingHintData.HintSize)
                    {
                        processor.Emit(OpCodes.Ldc_I4, 0);
                        currentHint++;
                    }
                    processor.Emit(OpCodes.Call, moduleDefinition.ImportReference(typeof(BindingHintData).GetMethod(nameof(BindingHintData.Create), BindingFlags.Public | BindingFlags.Static)));
                    processor.Emit(OpCodes.Stfld, hintFieldDefinition);
                    //RequireForUpdate
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldarg_0);
                    processor.Emit(OpCodes.Ldfld, queryFieldDefinition);
                    processor.Emit(OpCodes.Call, moduleDefinition.ImportReference(typeof(SystemState).GetMethod(nameof(SystemState.RequireForUpdate))));
                    processor.Emit(OpCodes.Ret);
                    onCreateMethodDefinition.Body.OptimizeMacros();
                    definition.Methods.Add(onCreateMethodDefinition);
                }
                private void GenerateOnDestroy(ModuleDefinition moduleDefinition)
                {
                    onDestroyMethodDefinition = new MethodDefinition("OnDestroy", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.ReuseSlot | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.Virtual, moduleDefinition.TypeSystem.Void);
                    var systemStateParameter = new ParameterDefinition("systemState", Mono.Cecil.ParameterAttributes.None, new ByReferenceType(moduleDefinition.ImportReference(typeof(SystemState))));
                    onDestroyMethodDefinition.Parameters.Add(systemStateParameter);
                    onDestroyMethodDefinition.Body.InitLocals = true;
                    onDestroyMethodDefinition.Body.SimplifyMacros();
                    var processor = onDestroyMethodDefinition.Body.GetILProcessor();
                    processor.Emit(OpCodes.Ret);
                    onDestroyMethodDefinition.Body.OptimizeMacros();
                    definition.Methods.Add(onDestroyMethodDefinition);
                }

                private void GenerateOnUpdate(ModuleDefinition moduleDefinition)
                {

                    onUpdateMethodDefinition = new MethodDefinition("OnUpdate", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.ReuseSlot | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.Virtual, moduleDefinition.TypeSystem.Void);
                    //onUpdateMethodDefinition.CustomAttributes.Add(new CustomAttribute(moduleDefinition.ImportReference(typeof(BurstCompileAttribute).GetConstructor(Type.EmptyTypes))));
                    var systemStateParameter = new ParameterDefinition("systemState", Mono.Cecil.ParameterAttributes.None, new ByReferenceType(moduleDefinition.ImportReference(typeof(SystemState))));
                    onUpdateMethodDefinition.Parameters.Add(systemStateParameter);
                    //Job Generic Instance
                    var baseJobTypeDefinition = moduleDefinition.ImportReference(typeof(UIBindingJob<,>));
                    var jobTypeInstance = new GenericInstanceType(baseJobTypeDefinition);
                    jobTypeInstance.GenericArguments.Add(componentType.typeDefinition);
                    jobTypeInstance.GenericArguments.Add(componentType.propertyElementTypeReference);
                    var jobTypeReference = moduleDefinition.ImportReference(jobTypeInstance);
                    //Variables
                    var jobVariable = new VariableDefinition(jobTypeReference);
                    var jobHandleVariable = new VariableDefinition(moduleDefinition.ImportReference(typeof(JobHandle)));
                    onUpdateMethodDefinition.Body.Variables.Add(jobVariable);
                    onUpdateMethodDefinition.Body.Variables.Add(jobHandleVariable);
                    //Methods
                    var getComponentTypeHandleGenericInstance = new GenericInstanceMethod(moduleDefinition.ImportReference(typeof(SystemState).GetGenericMethod(nameof(SystemState.GetComponentTypeHandle), BindingFlags.Public | BindingFlags.Instance)));
                    getComponentTypeHandleGenericInstance.GenericArguments.Add(componentType.typeDefinition);
                    var getComponentTypeHandleReference = moduleDefinition.ImportReference(getComponentTypeHandleGenericInstance);
                    var scheduleJob = new GenericInstanceMethod(moduleDefinition.ImportReference(typeof(JobEntityBatchExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public).First(m => m.Name == nameof(JobEntityBatchExtensions.Schedule) && m.GetParameters().Length == 3)));
                    scheduleJob.GenericArguments.Add(jobTypeReference);
                    var scheduleJobReference = moduleDefinition.ImportReference(scheduleJob);
                    onUpdateMethodDefinition.Body.InitLocals = true;
                    onUpdateMethodDefinition.Body.SimplifyMacros();
                    var processor = onUpdateMethodDefinition.Body.GetILProcessor();
                    //configBufferTypeHandle
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldc_I4_1);
                    processor.Emit(OpCodes.Call, moduleDefinition.ImportReference(typeof(SystemState).GetGenericMethod(nameof(SystemState.GetBufferTypeHandle), BindingFlags.Public | BindingFlags.Instance).MakeGenericMethod(typeof(UIConfigBufferData))));
                    //componentTypeHandle
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldc_I4_0);
                    processor.Emit(OpCodes.Call, getComponentTypeHandleReference);
                    //property
                    processor.Emit(OpCodes.Ldarg_0);
                    processor.Emit(OpCodes.Ldfld, propertyFieldDefinition);
                    //sourceTypeCode
                    processor.Emit(OpCodes.Ldc_I4, (int)componentType.sourceTypeCode);
                    //fieldOffset
                    processor.Emit(OpCodes.Ldc_I4, componentType.fieldOffset);
                    //vectorLength
                    processor.Emit(OpCodes.Ldc_I4, componentType.elementCount);
                    //BindingHintData
                    processor.Emit(OpCodes.Ldarg_0);
                    processor.Emit(OpCodes.Ldfld, hintFieldDefinition);
                    //BindingJob
                    processor.Emit(OpCodes.Newobj, moduleDefinition.ImportReference(jobTypeInstance.Resolve().Methods.First(m => m.IsConstructor)).MakeHostInstanceGeneric(componentType.typeDefinition, componentType.propertyElementTypeReference));
                    //Schedule
                    processor.Emit(OpCodes.Ldarg_0);
                    processor.Emit(OpCodes.Ldfld, queryFieldDefinition);
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Call, moduleDefinition.ImportReference(typeof(SystemState).GetProperty("Dependency", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetMethod));
                    processor.Emit(OpCodes.Call, scheduleJobReference);
                    processor.Emit(OpCodes.Stloc, jobHandleVariable);
                    //Complete
                    processor.Emit(OpCodes.Ldloca, jobHandleVariable);
                    processor.Emit(OpCodes.Call, moduleDefinition.ImportReference(typeof(JobHandle).GetMethod(nameof(JobHandle.Complete))));
                    processor.Emit(OpCodes.Ret);
                    onUpdateMethodDefinition.Body.OptimizeMacros();
                    definition.Methods.Add(onUpdateMethodDefinition);
                }


            }
            internal struct ConversionComponentInfo
            {
                public ConversionInfo conversionInfo;
                public TypeReference elementType;
                public int count;

            }
            internal readonly struct ConversionInfo
            {
                public readonly TypeCode elementTypeCode;
                public readonly int elementInputCount;
                public readonly int offset;
                public readonly int size;
                public readonly Type fieldType;
                public readonly MaterialPropertyFormat propertyFormat;
                public ConversionInfo(TypeCode elementTypeCode, int size, int elementInputCount, int offset = 0)
                {
                    this.elementTypeCode = elementTypeCode;
                    this.elementInputCount = elementInputCount;
                    this.offset = offset;
                    this.size = size;
                    switch (elementInputCount)
                    {
                        case 1:
                            this.fieldType = typeof(float);
                            this.propertyFormat = MaterialPropertyFormat.Float;
                            break;
                        case 2:
                            this.fieldType = typeof(float2);
                            this.propertyFormat = MaterialPropertyFormat.Float2;
                            break;
                        case 3:
                            this.fieldType = typeof(float3);
                            this.propertyFormat = MaterialPropertyFormat.Float3;
                            break;
                        case 4:
                            this.fieldType = typeof(float4);
                            this.propertyFormat = MaterialPropertyFormat.Float4;
                            break;
                        default:
                            this.fieldType = null;
                            this.propertyFormat = default;
                            break;
                    }
                }
                public bool IsValid { get => fieldType != null; }
                public ConversionInfo GetCompositeConversionInfo(int count)
                {
                    return new ConversionInfo(elementTypeCode, size, elementInputCount * count, offset);


                }

            }
        }
    }
}
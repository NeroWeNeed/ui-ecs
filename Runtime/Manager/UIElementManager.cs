using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;


[assembly: InternalsVisibleTo("NeroWeNeed.UIEcs.Editor")]
namespace NeroWeNeed.UIECS
{
    public static class UIElementNameUtility
    {

        public static string CreatePropertyName(string blockName, string name)
        {
            return string.IsNullOrWhiteSpace(blockName) ? name : $"{blockName}-{name}";
        }

        public static string CreatePropertyName(string blockName, string subName, string name, bool prefix)
        {
            return string.IsNullOrWhiteSpace(blockName) ? (string.IsNullOrWhiteSpace(subName) ? name : (prefix ? $"{subName}-{name}" : $"{name}-{subName}")) : (string.IsNullOrWhiteSpace(subName) ? $"{blockName}-{name}" : (prefix ? $"{blockName}-{subName}-{name}" : $"{blockName}-{name}-{subName}"));
        }
        public static string ToMaterialName(string propertyName)
        {
            var components = propertyName.Split('-');
            var sb = new StringBuilder("_");
            foreach (var component in components)
            {
                sb.Append(System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(component));
            }
            return sb.ToString();
        }
    }
    [BurstCompile]
    public static class UIElementManager
    {
        private sealed class PropertyBlockTypeIndexContext
        {
            private PropertyBlockTypeIndexContext() { }
        }
        private sealed class PropertyBlockTypeIndex<TPropertyBlock> where TPropertyBlock : struct
        {
            public static readonly SharedStatic<ulong> Ref = SharedStatic<ulong>.GetOrCreate<PropertyBlockTypeIndexContext, TPropertyBlock>();
        }
        private sealed class ElementTypeIndexContext
        {
            private ElementTypeIndexContext() { }
        }
        private sealed class ElementTypeIndex<TElement> where TElement : struct
        {
            public static readonly SharedStatic<ulong> Ref = SharedStatic<ulong>.GetOrCreate<ElementTypeIndexContext, TElement>();
        }
        internal static readonly Type[] elementTypeInterfaces = new Type[] {
            typeof(IUIElementConstrain),
            typeof(IUIElementLayout),
            typeof(IUIElementSize),
            typeof(IUIElementGenerateMeshData)
        };
        internal static readonly Dictionary<string, ulong> elementHashes = new Dictionary<string, ulong>();
        internal static readonly Dictionary<ulong, Type> elementTypes = new Dictionary<ulong, Type>();
        internal static readonly Dictionary<Type, ulong> propertyBlockHashes = new Dictionary<Type, ulong>();
        internal static readonly Dictionary<ulong, Type> propertyBlockTypes = new Dictionary<ulong, Type>();
        internal static readonly Dictionary<string, UIProperty> properties = new Dictionary<string, UIProperty>();
        internal static readonly Dictionary<UIProperty, Type> propertyTypes = new Dictionary<UIProperty, Type>();
        internal static readonly Dictionary<UIProperty, Type> assetProperties = new Dictionary<UIProperty, Type>();
        internal static NativeMultiHashMap<ulong, UILengthInfo> uiLengthNormalizationInfo;
        internal static NativeMultiHashMap<ulong, UIElementPropertyBlock> elementPropertyBlocks;
        internal static NativeList<ulong> globalPropertyBlocks;
        internal static NativeHashMap<ulong, UIElementInfo> elementInfo;

        internal static bool appDomainUnloadRegistered;
        internal static bool initialized;
        internal static bool functionPointersInitialized;
        [BurstDiscard]
#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
#endif
        internal static void Initialize()
        {
            Initialize(true);
        }

        internal static void Initialize(bool initializeWithFunctionPointers)
        {
            if (!initialized)
            {
                Profiler.BeginSample($"{nameof(UIElementManager)} Initialization");
                if (!appDomainUnloadRegistered)
                {


                    AppDomain.CurrentDomain.DomainUnload += delegate
                    {
                        if (initialized)
                        {
                            Shutdown();
                        }
                    };
                    appDomainUnloadRegistered = true;
                }
                var context = new InitializationContext
                {
                    normalizationInfo = new List<InitializationContext.UILengthNormalizationInfo>()
                };
                uiLengthNormalizationInfo = new NativeMultiHashMap<ulong, UILengthInfo>(8, Allocator.Persistent);
                elementPropertyBlocks = new NativeMultiHashMap<ulong, UIElementPropertyBlock>(8, Allocator.Persistent);
                globalPropertyBlocks = new NativeList<ulong>(8, Allocator.Persistent);
                elementInfo = new NativeHashMap<ulong, UIElementInfo>(8, Allocator.Persistent);
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            if (typeof(IPropertyBlock).IsAssignableFrom(type) && UnsafeUtility.IsUnmanaged(type))
                            {
                                InitializePropertyBlockTypeIndex(type, ref context);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Skipping {assembly.FullName}: {e.Message}");
                    }
                }
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            if (typeof(IUINode).IsAssignableFrom(type) && UnsafeUtility.IsUnmanaged(type))
                            {
                                InitializeUIElementIndex(type, initializeWithFunctionPointers);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Skipping {assembly.FullName}: {e.Message}");
                    }
                }
                foreach (var info in context.normalizationInfo)
                {
                    if (!string.IsNullOrWhiteSpace(info.referenceProperty) && properties.TryGetValue(info.referenceProperty, out UIProperty referenceProperty))
                    {
                        uiLengthNormalizationInfo.Add(info.property.blockHash, new UILengthInfo(info.property, referenceProperty, info.referenceTarget));
                    }
                    else
                    {
                        uiLengthNormalizationInfo.Add(info.property.blockHash, new UILengthInfo(info.property, default, ReferencePropertyTarget.None));
                    }
                }
                initialized = true;
                functionPointersInitialized = initializeWithFunctionPointers;
                Profiler.EndSample();
            }
        }
        [BurstDiscard]
        internal static void Shutdown()
        {
            properties.Clear();
            propertyBlockHashes.Clear();
            uiLengthNormalizationInfo.Dispose();
            elementPropertyBlocks.Dispose();
            globalPropertyBlocks.Dispose();
            elementInfo.Dispose();
            initialized = false;
        }
        [BurstDiscard]
        internal static void InitializeFunctionPointers()
        {
            if (!functionPointersInitialized)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            if (typeof(IUINode).IsAssignableFrom(type) && UnsafeUtility.IsUnmanaged(type))
                            {
                                var hash = TypeHash.CalculateStableTypeHash(type);
                                elementInfo[hash] = new UIElementInfo(type);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Skipping {assembly.FullName}: {e.Message}");
                    }
                }
                functionPointersInitialized = true;
            }
        }
        [BurstDiscard]
        private static void InitializeUIElementIndex(Type type, bool withFunctionPointer)
        {

            var hash = TypeHash.CalculateStableTypeHash(type);
            elementHashes[type.FullName] = hash;
            elementTypes[hash] = type;
            if (withFunctionPointer)
                elementInfo[hash] = new UIElementInfo(type);
            typeof(UIElementManager).GetMethod(nameof(InitializeElementTypeHash), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).MakeGenericMethod(type).Invoke(null, new object[] { hash });

            foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                if (typeof(IPropertyBlockField).IsAssignableFrom(field.FieldType))
                {
                    var propertyType = field.FieldType.GenericTypeArguments[0];
                    elementPropertyBlocks.Add(hash, new UIElementPropertyBlock(propertyBlockHashes[propertyType], !typeof(IPropertyBlockOptionalField).IsAssignableFrom(field.FieldType)));
                }
            }
        }

        [BurstDiscard]
        private static void InitializePropertyBlockTypeIndex(Type type, ref InitializationContext context)
        {
            var hash = TypeHash.CalculateStableTypeHash(type);
            //var componentType = ComponentType.ReadOnly(typeof(UIPropertyBlockTag<>).MakeGenericType(type));
            propertyBlockHashes[type] = hash;
            //propertyBlockComponentTags[hash] = componentType;
            propertyBlockTypes[hash] = type;
            typeof(UIElementManager).GetMethod(nameof(InitializePropertyBlockTypeHash), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).MakeGenericMethod(type).Invoke(null, new object[] { hash });
            var propertyBlockAttribute = type.GetCustomAttribute<PropertyBlockAttribute>();
            if (propertyBlockAttribute?.global == true)
            {
                globalPropertyBlocks.Add(hash);
            }
            InitializeProperties(type, hash, propertyBlockAttribute, ref context);
        }
        [BurstDiscard]
        private static void InitializeProperties(Type type, ulong hash, PropertyBlockAttribute propertyBlockAttribute, ref InitializationContext context)
        {
            var blockName = propertyBlockAttribute?.name;
            UIProperty property;
            foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<IgnorePropertyAttribute>() == null)
                {
                    if (typeof(IUIBool).IsAssignableFrom(field.FieldType) && field.FieldType.IsValueType)
                    {
                        var attr = field.GetCustomAttribute<UIBoolAttribute>();
                        if (attr != null)
                        {
                            var basePropertyName = field.GetCustomAttribute<PropertyAttribute>()?.name ?? field.Name;
                            var fieldOffset = UnsafeUtility.GetFieldOffset(field);
                            var fieldSize = UnsafeUtility.SizeOf(field.FieldType);
                            for (int bitOffset = 0; bitOffset < attr.names.Length && bitOffset < fieldSize * 8; bitOffset++)
                            {
                                var name = attr.names[bitOffset];
                                property = new UIProperty(hash, fieldOffset, fieldSize, bitOffset);
                                var fullPropertyName = CreatePropertyName(blockName, name);
                                properties.Add(fullPropertyName, property);
                                propertyTypes[property] = typeof(bool);
                                RegisterExtraPropertyData(type, field, field, property, fullPropertyName, ref context);
                            }
                        }
                    }
                    else
                    {
                        var basePropertyName = field.GetCustomAttribute<PropertyAttribute>()?.name ?? field.Name;
                        var fieldOffset = UnsafeUtility.GetFieldOffset(field);
                        property = new UIProperty(hash, fieldOffset, UnsafeUtility.SizeOf(field.FieldType));
                        var fullPropertyName = CreatePropertyName(blockName, basePropertyName);
                        properties.Add(fullPropertyName, property);
                        propertyTypes[property] = field.FieldType;
                        RegisterExtraPropertyData(type, field, field, property, fullPropertyName, ref context);
                        if (typeof(ICompositeData).IsAssignableFrom(field.FieldType) && field.FieldType.IsValueType)
                        {
                            var compositeNameAttribute = field.GetCustomAttribute<CompositeNameAttribute>();
                            var compositeFields = field.FieldType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            var compositeType = field.FieldType.GenericTypeArguments[0];
                            var compositeTypeSize = UnsafeUtility.SizeOf(compositeType);
                            foreach (var compositeField in compositeFields)
                            {
                                string compositeFieldName;
                                switch (compositeField.Name)
                                {
                                    case "x":
                                        compositeFieldName = compositeNameAttribute?.xName;
                                        break;
                                    case "y":
                                        compositeFieldName = compositeNameAttribute?.yName;
                                        break;
                                    case "z":
                                        compositeFieldName = compositeNameAttribute?.zName;
                                        break;
                                    case "w":
                                        compositeFieldName = compositeNameAttribute?.wName;
                                        break;
                                    default:
                                        continue;
                                }
                                if (string.IsNullOrWhiteSpace(compositeFieldName))
                                    compositeFieldName = field.Name;
                                property = new UIProperty(hash, fieldOffset + UnsafeUtility.GetFieldOffset(compositeField), compositeTypeSize);
                                fullPropertyName = CreatePropertyName(blockName, compositeFieldName, basePropertyName, compositeNameAttribute?.Prefix ?? true);
                                properties.Add(fullPropertyName, property);
                                propertyTypes[property] = compositeType;
                                RegisterExtraPropertyData(type, compositeField, field, property, fullPropertyName, ref context);
                            }

                        }
                    }
                }
            }
        }
        [BurstDiscard]
        private static void RegisterExtraPropertyData(Type propertyBlock, FieldInfo fieldInfo, FieldInfo attributeTargetFieldInfo, UIProperty property, string name, ref InitializationContext context)
        {
            if (fieldInfo.FieldType == typeof(UILength))
            {
                var referencePropertyAttribute = attributeTargetFieldInfo.GetCustomAttribute<ReferencePropertyAttribute>();

                context.normalizationInfo.Add(new InitializationContext.UILengthNormalizationInfo
                {
                    property = property,
                    referenceProperty = referencePropertyAttribute?.Name,
                    referenceTarget = referencePropertyAttribute?.Target ?? ReferencePropertyTarget.None
                });


            }
            if (fieldInfo.FieldType == typeof(UIUnityObjectAsset))
            {
                var assetAttr = attributeTargetFieldInfo.GetCustomAttribute<UIUnityObjectAssetAttribute>();
                assetProperties[property] =  assetAttr?.type ?? typeof(UnityEngine.Object);

            }
        }
        [BurstDiscard]
        public static Type GetAssetType(UIProperty property)
        {
            return assetProperties[property];
        }
        [BurstDiscard]
        public static bool TryGetAssetType(UIProperty property,out Type result)
        {
            return assetProperties.TryGetValue(property, out result);
            
        }
        [BurstDiscard]
        internal static void InitializePropertyBlockTypeHash<TPropertyBlock>(ulong index) where TPropertyBlock : unmanaged, IPropertyBlock
        {
            PropertyBlockTypeIndex<TPropertyBlock>.Ref.Data = index;
        }
        [BurstDiscard]
        internal static void InitializeElementTypeHash<TElement>(ulong index) where TElement : unmanaged, IUINode
        {
            ElementTypeIndex<TElement>.Ref.Data = index;
        }
        [BurstDiscard]
        public static Type GetElement(ulong hash)
        {
            return elementTypes[hash];
        }
        [BurstDiscard]
        public static ulong GetElement(Type type)
        {
            return elementHashes[type.FullName];
        }
        [BurstDiscard]
        public static bool TryGetElement(Type type, out ulong hash)
        {
            return elementHashes.TryGetValue(type.FullName, out hash);
        }
        [BurstDiscard]
        public static bool TryGetElement(string name, out ulong hash)
        {
            return elementHashes.TryGetValue(name, out hash);
        }
        [BurstDiscard]
        public static ulong GetElement(string name)
        {
            return elementHashes[name];
        }

        [BurstDiscard]
        public static ulong GetPropertyBlock(Type type)
        {
            return propertyBlockHashes[type];
        }
        [BurstDiscard]
        public static UIProperty GetProperty(string name)
        {
            return properties[name];
        }
        [BurstDiscard]
        public static Type GetPropertyType(UIProperty property)
        {
            return propertyTypes[property];
        }
        [BurstDiscard]
        public static Type GetPropertyBlock(ulong hash)
        {
            return propertyBlockTypes[hash];
        }

        [BurstDiscard]
        public static FieldInfo GetField(string name)
        {
            return GetField(name, out var _);
        }
        [BurstDiscard]
        public static FieldInfo GetField(string name, out Type container)
        {
            var property = GetProperty(name);
            var blockType = GetPropertyBlock(property.blockHash);
            foreach (var field in blockType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                var offset = UnsafeUtility.GetFieldOffset(field);
                if (offset == property.offset && property.length == UnsafeUtility.SizeOf(field.FieldType))
                {
                    container = blockType;
                    return field;
                }
                else if (offset >= property.offset && offset < property.offset + property.length)
                {
                    foreach (var subField in field.FieldType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                    {
                        var subOffset = UnsafeUtility.GetFieldOffset(subField);
                        if ((offset + subOffset) == property.offset && property.length == UnsafeUtility.SizeOf(subField.FieldType))
                        {
                            container = field.FieldType;
                            return subField;
                        }
                    }
                }
            }
            container = null;
            return null;

        }


        [BurstDiscard]
        internal static string CreatePropertyName(string blockName, string name)
        {
            return string.IsNullOrWhiteSpace(blockName) ? name : $"{blockName}-{name}";
        }
        [BurstDiscard]
        internal static string CreatePropertyName(string blockName, string subName, string name, bool prefix)
        {
            return string.IsNullOrWhiteSpace(blockName) ? (string.IsNullOrWhiteSpace(subName) ? name : (prefix ? $"{subName}-{name}" : $"{name}-{subName}")) : (string.IsNullOrWhiteSpace(subName) ? $"{blockName}-{name}" : (prefix ? $"{blockName}-{subName}-{name}" : $"{blockName}-{name}-{subName}"));
        }

        [BurstDiscard]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Type GetWrapperType(Type type) => type.Assembly.GetType(GetWrapperTypeName(type));
        [BurstDiscard]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetWrapperTypeName(Type type) => $"{type.FullName}$UIWrapper";

        [BurstCompile]
        public static ulong GetPropertyBlock<TPropertyBlock>() where TPropertyBlock : unmanaged, IPropertyBlock
        {
            return PropertyBlockTypeIndex<TPropertyBlock>.Ref.Data;
        }
        [BurstCompile]
        public static ulong GetElement<TElement>() where TElement : unmanaged, IUINode
        {
            return PropertyBlockTypeIndex<TElement>.Ref.Data;
        }

        public readonly struct UILengthInfo
        {
            public readonly UIProperty property;
            public readonly UIProperty referenceProperty;
            public readonly ReferencePropertyTarget target;

            public UILengthInfo(UIProperty property, UIProperty referenceProperty, ReferencePropertyTarget target)
            {
                this.property = property;
                this.referenceProperty = referenceProperty;
                this.target = target;
            }
        }
        public readonly struct UIElementInfo
        {
            public readonly FunctionPointer<Constrain> constrain;
            public readonly FunctionPointer<Layout> layout;
            public readonly FunctionPointer<GenerateMeshData> generateMeshData;
            public readonly FunctionPointer<Size> size;
            public bool IsTerminal { get => !constrain.IsCreated && !layout.IsCreated; }
            public UIElementInfo(Type type)
            {
                var wrapperType = GetWrapperType(type);
                if (wrapperType != null)
                {
                    if (typeof(IUIElementConstrain).IsAssignableFrom(type))
                    {
                        constrain = BurstCompiler.CompileFunctionPointer((Constrain)wrapperType.GetMethod(nameof(IUIElementConstrain.Constrain), BindingFlags.Public | BindingFlags.Static).CreateDelegate(typeof(Constrain)));
                    }
                    else
                    {
                        constrain = default;
                    }
                    if (typeof(IUIElementLayout).IsAssignableFrom(type))
                    {
                        layout = BurstCompiler.CompileFunctionPointer((Layout)wrapperType.GetMethod(nameof(IUIElementLayout.Layout), BindingFlags.Public | BindingFlags.Static).CreateDelegate(typeof(Layout)));
                    }
                    else
                    {
                        layout = default;
                    }
                    if (typeof(IUIElementGenerateMeshData).IsAssignableFrom(type))
                    {
                        generateMeshData = BurstCompiler.CompileFunctionPointer((GenerateMeshData)wrapperType.GetMethod(nameof(IUIElementGenerateMeshData.GenerateMeshData), BindingFlags.Public | BindingFlags.Static).CreateDelegate(typeof(GenerateMeshData)));
                    }
                    else
                    {
                        generateMeshData = default;
                    }
                    if (typeof(IUIElementSize).IsAssignableFrom(type))
                    {
                        size = BurstCompiler.CompileFunctionPointer((Size)wrapperType.GetMethod(nameof(IUIElementSize.Size), BindingFlags.Public | BindingFlags.Static).CreateDelegate(typeof(Size)));
                    }
                    else
                    {
                        size = default;
                    }
                }
                else
                {
                    constrain = default;
                    layout = default;
                    size = default;
                    generateMeshData = default;
                }

            }

            public UIElementInfo(FunctionPointer<Constrain> constrain, FunctionPointer<Layout> layout, FunctionPointer<GenerateMeshData> generateMeshData, FunctionPointer<Size> size)
            {
                this.constrain = constrain;
                this.layout = layout;
                this.generateMeshData = generateMeshData;
                this.size = size;
            }
        }
        private struct InitializationContext
        {
            public List<UILengthNormalizationInfo> normalizationInfo;
            public struct UILengthNormalizationInfo
            {
                public UIProperty property;
                public string referenceProperty;
                public ReferencePropertyTarget referenceTarget;
            }

        }
    }
    [Serializable]
    public readonly struct UIElementPropertyBlock : IEquatable<UIElementPropertyBlock>
    {
        public readonly ulong blockHash;
        public readonly byte required;
        public bool IsRequired { get => required != 0; }
        public UIElementPropertyBlock(ulong blockHash, bool required)
        {
            this.blockHash = blockHash;
            this.required = (byte)(required ? 1 : 0);
        }

        public override bool Equals(object obj)
        {
            return obj is UIElementPropertyBlock block &&
                   blockHash == block.blockHash &&
                   required == block.required;
        }

        public bool Equals(UIElementPropertyBlock other)
        {
            return blockHash == other.blockHash &&
                   required == other.required;
        }

        public override int GetHashCode()
        {
            int hashCode = 213696550;
            hashCode = hashCode * -1521134295 + blockHash.GetHashCode();
            hashCode = hashCode * -1521134295 + required.GetHashCode();
            return hashCode;
        }
        public override string ToString()
        {
            return $"{nameof(UIElementPropertyBlock)}({nameof(blockHash)}: {blockHash}, {nameof(required)}: {IsRequired})";
        }
    }
    [Serializable]
    public readonly struct UIProperty : IEquatable<UIProperty>
    {
        public readonly ulong blockHash;
        public readonly int offset;
        public readonly int length;
        public readonly int bitOffset;
        public bool IsCreated { get => !(blockHash == 0 && offset == 0 && length == 0 && bitOffset == 0); }
        public bool IsBitProperty { get => bitOffset >= 0; }

        internal UIProperty(ulong blockHash, int offset, int length, int bitOffset = -1)
        {
            this.blockHash = blockHash;
            this.offset = offset;
            this.length = length;
            this.bitOffset = bitOffset;
        }
        public override bool Equals(object obj)
        {
            return obj is UIProperty property &&
                   blockHash == property.blockHash &&
                   offset == property.offset &&
                   length == property.length &&
                   bitOffset == property.bitOffset;
        }

        public bool Equals(UIProperty other)
        {
            return blockHash == other.blockHash &&
                    offset == other.offset &&
                    length == other.length &&
                    bitOffset == other.bitOffset;
        }

        public override int GetHashCode()
        {
            int hashCode = -10912667;
            hashCode = hashCode * -1521134295 + blockHash.GetHashCode();
            hashCode = hashCode * -1521134295 + offset.GetHashCode();
            hashCode = hashCode * -1521134295 + length.GetHashCode();
            hashCode = hashCode * -1521134295 + bitOffset.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"{nameof(UIProperty)}({nameof(blockHash)}: {blockHash}, {nameof(offset)}: {offset}, {nameof(length)}: {length})";
        }
    }
}


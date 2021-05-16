using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace NeroWeNeed.UIECS.Editor
{
    public static class UIConversionComponentManager
    {
        private static readonly Dictionary<string, ComponentType> components = new Dictionary<string, ComponentType>();
        private static readonly Dictionary<ulong, string[]> bindings = new Dictionary<ulong, string[]>();
        internal static readonly Dictionary<ulong, ComponentType> propertyBlockTags = new Dictionary<ulong, ComponentType>();

        internal static readonly Dictionary<string, ModelInfo> modelComponents = new Dictionary<string, ModelInfo>();

        internal static readonly Dictionary<Type, List<IUIModelPropertyInitializer>> propertyInitializers = new Dictionary<Type, List<IUIModelPropertyInitializer>>();


        private static bool initialized;
        static UIConversionComponentManager()
        {
            Initialize();
        }
        private static void Initialize()
        {
            if (!initialized)
            {

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            if (typeof(IUIMaterialProperty).IsAssignableFrom(type) && type.IsValueType)
                            {
                                var attr = type.GetCustomAttribute<UIMaterialPropertyComponentAttribute>();
                                if (attr != null)
                                {
                                    components[attr.Name] = type;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error in Assembly {assembly.FullName}: {e.Message} {e.StackTrace}");
                    }
                }
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            if (typeof(IPropertyBlock).IsAssignableFrom(type) && type.IsValueType)
                            {
                                propertyBlockTags[TypeHash.CalculateStableTypeHash(type)] = ComponentType.ReadWrite(typeof(UIPropertyBlockTag<>).MakeGenericType(type));

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error in Assembly {assembly.FullName}: {e.Message} {e.StackTrace}");
                    }
                }

                InitializeModelComponents();
                InitializePropertyInitializers();
                var temp = new Dictionary<ulong, List<string>>();
                foreach (var component in components.Keys)
                {
                    var property = UIElementManager.GetProperty(component);
                    if (!temp.TryGetValue(property.blockHash, out var l))
                    {
                        l = new List<string>();
                        temp[property.blockHash] = l;
                    }
                    l.Add(component);
                }
                foreach (var t in temp)
                {
                    bindings[t.Key] = t.Value.ToArray();
                }
                initialized = true;
            }
        }
        private static void InitializeModelComponents()
        {


            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var attr in assembly.GetCustomAttributes<UIModelPropertyAttribute>())
                    {
                        if (attr != null && (typeof(IComponentData).IsAssignableFrom(attr.target) || typeof(IBufferElementData).IsAssignableFrom(attr.target)))
                        {
                            if (!modelComponents.TryGetValue(attr.modelName, out var modelInfo))
                            {
                                modelInfo = new ModelInfo(attr.modelName);
                                modelComponents[attr.modelName] = modelInfo;
                            }
                            if (!modelInfo.properties.TryGetValue(attr.propertyName, out var propertyInfo))
                            {
                                propertyInfo = new ModelInfo.PropertyInfo(attr.propertyName);
                                modelInfo.properties[attr.propertyName] = propertyInfo;
                            }
                            switch (attr.targetType)
                            {
                                case PropertyComponentTargetType.Model:
                                    modelInfo.components.Add(attr.target);
                                    break;
                                case PropertyComponentTargetType.BindingNode:
                                    propertyInfo.components.Add(attr.target);
                                    break;
                                case PropertyComponentTargetType.BindingNodeChildren:
                                    propertyInfo.childrenComponents.Add(attr.target);
                                    break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in Assembly {assembly.FullName}: {e.Message} {e.StackTrace}");
                }
            }
        }

        private static void InitializePropertyInitializers()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(IUIModelPropertyInitializer).IsAssignableFrom(type))
                        {
                            var attrs = type.GetCustomAttributes<UIModelPropertyInitializerAttribute>();
                            if (attrs != null)
                            {
                                foreach (var attr in attrs)
                                {
                                    if (!propertyInitializers.TryGetValue(attr.value, out var propertyInitializerList))
                                    {
                                        propertyInitializerList = new List<IUIModelPropertyInitializer>();
                                        propertyInitializers[attr.value] = propertyInitializerList;
                                    }
                                    propertyInitializerList.Add((IUIModelPropertyInitializer)Activator.CreateInstance(type));
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in Assembly {assembly.FullName}: {e.Message} {e.StackTrace}");
                }
            }
        }
        public static bool TryGetBindingComponents(ulong block, out string[] componentTypes)
        {
            if (bindings.TryGetValue(block, out var t))
            {
                componentTypes = t;
                return true;
            }
            else
            {
                componentTypes = null;
                return false;
            }
        }
        public static ComponentType GetBindingComponent(string name)
        {
            return components[name];
        }
        public static string[] GetBindingComponents(ulong block)
        {
            return bindings[block];
        }
        public static ComponentType GetPropertyBlockTag(ulong hash)
        {
            return propertyBlockTags[hash];
        }
        public static ModelInfo GetModelInfo(string modelName)
        {
            return modelComponents[modelName];
        }
        public static void InitializePropertyComponents(Type component, EntityManager entityManager, int index, UIViewAsset viewAsset, NativeArray<Entity> entities)
        {
            if (propertyInitializers.TryGetValue(component, out var initializers))
            {
                foreach (var initializer in initializers)
                {
                    initializer.Initialize(component, entityManager, index, viewAsset, entities);
                }
            }
            if (component.IsConstructedGenericType && propertyInitializers.TryGetValue(component.GetGenericTypeDefinition(), out initializers))
            {
                foreach (var initializer in initializers)
                {
                    initializer.Initialize(component, entityManager, index, viewAsset, entities);
                }
            }
        }
        public struct MaterialBindingComponent
        {
            public ComponentType componentType;
            public string materialPropertyName;
        }
        public class ModelInfo
        {
            public string name;
            public Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();
            public List<Type> components = new List<Type>();
            public ModelInfo(string name)
            {
                this.name = name;
            }
            public class PropertyInfo
            {
                public string name;
                public List<Type> components = new List<Type>();
                public List<Type> childrenComponents = new List<Type>();
                public PropertyInfo(string name)
                {
                    this.name = name;
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NeroWeNeed.UIECS.Editor
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class UIProcessorAttribute : Attribute
    {
        public Type type;

        public UIProcessorAttribute(Type type)
        {
            this.type = type;
        }
    }
    public unsafe interface IPropertyBlockPreprocessor
    {
        public void Preprocess(UIModelConfigPropertyBlockHeader* header,void* config, int length,PropertyBlockProcessorContext context);
    }
    public unsafe interface IUIElementPreprocessor
    {
        public void Preprocess(void* config, int length);
    }
    public unsafe interface IPropertyBlockPostprocessor
    {
        public void Postprocess(UIModelConfigPropertyBlockHeader* header, void* config, int length, PropertyBlockProcessorContext context);
    }
    public unsafe interface IUIElementPostprocessor
    {
        public void Postprocess(void* config, int length);
    }
    public static class UIECSProcessorManager
    {
        internal static readonly Dictionary<Type, IUIElementPostprocessor> elementPostprocessors = new Dictionary<Type, IUIElementPostprocessor>();
        internal static readonly Dictionary<Type, IUIElementPreprocessor> elementPreprocessors = new Dictionary<Type, IUIElementPreprocessor>();
        internal static readonly Dictionary<Type, IPropertyBlockPostprocessor> propertyBlockPostprocessors = new Dictionary<Type, IPropertyBlockPostprocessor>();
        internal static readonly Dictionary<Type, IPropertyBlockPreprocessor> propertyBlockPreprocessors = new Dictionary<Type, IPropertyBlockPreprocessor>();
        static UIECSProcessorManager()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass || type.IsValueType)
                    {
                        var attr = type.GetCustomAttribute<UIProcessorAttribute>();
                        if (attr != null)
                        {
                            object instance = default;
                            bool instanceCreated = false;
                            if (typeof(IPropertyBlockPreprocessor).IsAssignableFrom(type))
                            {
                                if (!instanceCreated)
                                {
                                    instance = Activator.CreateInstance(type);
                                    instanceCreated = true;
                                }
                                propertyBlockPreprocessors[attr.type] = (IPropertyBlockPreprocessor)instance;
                            }
                            if (typeof(IPropertyBlockPostprocessor).IsAssignableFrom(type))
                            {
                                if (!instanceCreated)
                                {
                                    instance = Activator.CreateInstance(type);
                                    instanceCreated = true;
                                }
                                propertyBlockPostprocessors[attr.type] = (IPropertyBlockPostprocessor)instance;
                            }
                            if (typeof(IUIElementPreprocessor).IsAssignableFrom(type))
                            {
                                if (!instanceCreated)
                                {
                                    instance = Activator.CreateInstance(type);
                                    instanceCreated = true;
                                }
                                elementPreprocessors[attr.type] = (IUIElementPreprocessor)instance;
                            }
                            if (typeof(IUIElementPostprocessor).IsAssignableFrom(type))
                            {
                                if (!instanceCreated)
                                {
                                    instance = Activator.CreateInstance(type);
                                }
                                elementPostprocessors[attr.type] = (IUIElementPostprocessor)instance;
                            }
                        }
                    }
                }
            }
        }


    }
}
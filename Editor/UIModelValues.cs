using System;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using NeroWeNeed.Commons;
using Unity.Entities;

namespace NeroWeNeed.UIECS.Editor
{
    public abstract class UIModelAssetBaseProperty
    {
        public string name;
        public bool array;
    }
    public sealed class UIModelAssetValueProperty : UIModelAssetBaseProperty
    {
        public SerializableType valueType;

        public UIModelAssetValueProperty()
        {
        }

        public UIModelAssetValueProperty(string name, bool array, SerializableType valueType)
        {
            this.name = name;
            this.array = array;
            this.valueType = valueType;
        }
    }
    public sealed class UIModelAssetStringProperty : UIModelAssetBaseProperty
    {


        public UIModelAssetStringProperty()
        {
        }

        public UIModelAssetStringProperty(string name)
        {
            this.name = name;
            this.array = false;
        }
    }
    public sealed class UIModelAssetModelProperty : UIModelAssetBaseProperty
    {
        public UIModelAsset modelAsset;

        public UIModelAssetModelProperty()
        {
        }

        public UIModelAssetModelProperty(string name, SerializableType componentType, UIModelAsset modelAsset)
        {
            this.name = name;
            this.modelAsset = modelAsset;
        }
    }
    public sealed class UIModelArrayAssetModelProperty : UIModelAssetBaseProperty
    {
        public UIModelAsset modelAsset;

        public UIModelArrayAssetModelProperty()
        {
        }

        public UIModelArrayAssetModelProperty(string name, SerializableType componentType, UIModelAsset modelAsset)
        {
            this.name = name;
            this.modelAsset = modelAsset;
        }
    }
    public interface IUIModelPropertyProvider
    {
        public UIModelAssetBaseProperty CreateProperty(UIModel model, UIModel.Property property);
        public TypeDefinition CreateType(UIModel model, UIModel.Property property, AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition);
    }
    public class UIModelValuePropertyProvider : IUIModelPropertyProvider
    {
        public UIModelAssetBaseProperty CreateProperty(UIModel model, UIModel.Property property)
        {

            var isArray = false;
            var baseTypeName = property.ResolvedType;
            if (baseTypeName.EndsWith("[]"))
            {
                isArray = true;
                baseTypeName = baseTypeName.Substring(0, baseTypeName.Length - 2);
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(baseTypeName, false);
                if (type != null)
                {
                    return new UIModelAssetValueProperty(property.name, isArray, type);
                }
            }
            return null;

        }
        private Type GetValueType(UIModel.Property property, out bool isArray)
        {
            isArray = false;
            var baseTypeName = property.ResolvedType;
            if (baseTypeName.EndsWith("[]"))
            {
                isArray = true;
                baseTypeName = baseTypeName.Substring(0, baseTypeName.Length - 2);
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(baseTypeName, false);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        public TypeDefinition CreateType(UIModel model, UIModel.Property property, AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition)
        {
            var type = new TypeDefinition(model.@namespace, $"UIModelProperty_{model.name}_{property.name}", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.Serializable, moduleDefinition.ImportReference(typeof(ValueType)));
            var fieldType = GetValueType(property, out var isBuffer);
            var field = new FieldDefinition("value", FieldAttributes.Public, moduleDefinition.ImportReference(fieldType));
            type.Fields.Add(field);
            GenericInstanceType bindingType;
            var bindingAttribute = new CustomAttribute(moduleDefinition.ImportReference(typeof(UIModelPropertyAttribute).GetConstructor(new Type[] { typeof(string), typeof(string), typeof(Type), typeof(PropertyComponentTargetType) })));
            if (isBuffer)
            {
                type.Interfaces.Add(new InterfaceImplementation(moduleDefinition.ImportReference(typeof(IBufferElementData))));
                var internalBufferCapacityAttribute = new CustomAttribute(moduleDefinition.ImportReference(typeof(InternalBufferCapacityAttribute).GetConstructor(new Type[] { typeof(int) })));
                internalBufferCapacityAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.Int32, 0));
                type.CustomAttributes.Add(internalBufferCapacityAttribute);
                //Binding Node
                bindingType = moduleDefinition.ImportReference(typeof(UIValueNodeBufferBinding<,>)).MakeGenericInstanceType(type, field.FieldType);
            }
            else
            {
                type.Interfaces.Add(new InterfaceImplementation(moduleDefinition.ImportReference(typeof(IComponentData))));
                //Binding Node
                bindingType = moduleDefinition.ImportReference(typeof(UIValueNodeComponentBinding<,>)).MakeGenericInstanceType(type, field.FieldType);
            }
            bindingAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, model.name));
            bindingAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, property.name));
            bindingAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.TypedReference, bindingType));
            bindingAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.Byte, (byte)PropertyComponentTargetType.BindingNode));
            assemblyDefinition.CustomAttributes.Add(bindingAttribute);
            //Binding Node Type
            //RegisterGenericComponentType
            var genericTypeAttribute = new CustomAttribute(moduleDefinition.ImportReference(typeof(RegisterGenericComponentTypeAttribute).GetConstructor(new Type[] { typeof(Type) })));
            genericTypeAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.TypedReference, bindingType));
            assemblyDefinition.CustomAttributes.Add(genericTypeAttribute);
            //Model Node
            var modelPropertyAttribute = new CustomAttribute(moduleDefinition.ImportReference(typeof(UIModelPropertyAttribute).GetConstructor(new Type[] { typeof(string), typeof(string), typeof(Type), typeof(PropertyComponentTargetType) })));
            modelPropertyAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, model.name));
            modelPropertyAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, property.name));
            modelPropertyAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.TypedReference, type));
            modelPropertyAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.Byte, (byte)PropertyComponentTargetType.Model));
            assemblyDefinition.CustomAttributes.Add(modelPropertyAttribute);
            return type;

        }
    }

    public class UIModelStringPropertyProvider : IUIModelPropertyProvider
    {
        public UIModelAssetBaseProperty CreateProperty(UIModel model, UIModel.Property property)
        {
            return new UIModelAssetStringProperty(property.name);
        }

        public TypeDefinition CreateType(UIModel model, UIModel.Property property, AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition)
        {
            var type = new TypeDefinition(model.@namespace, $"UIModelProperty_{model.name}_{property.name}", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.Serializable, moduleDefinition.ImportReference(typeof(ValueType)));
            var field = new FieldDefinition("value", FieldAttributes.Public, moduleDefinition.TypeSystem.Byte);
            type.Fields.Add(field);
            type.Interfaces.Add(new InterfaceImplementation(moduleDefinition.ImportReference(typeof(IBufferElementData))));
            var internalBufferCapacityAttribute = new CustomAttribute(moduleDefinition.ImportReference(typeof(InternalBufferCapacityAttribute).GetConstructor(new Type[] { typeof(int) })));
            internalBufferCapacityAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.Int32, 0));
            type.CustomAttributes.Add(internalBufferCapacityAttribute);
            var modelPropertyAttribute = new CustomAttribute(moduleDefinition.ImportReference(typeof(UIModelPropertyAttribute).GetConstructor(new Type[] { typeof(string), typeof(string), typeof(Type), typeof(PropertyComponentTargetType) })));
            modelPropertyAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, model.name));
            modelPropertyAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, property.name));
            modelPropertyAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.TypedReference, type));
            modelPropertyAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.Byte, (byte)PropertyComponentTargetType.Model));
            assemblyDefinition.CustomAttributes.Add(modelPropertyAttribute);
            var bindingAttribute = new CustomAttribute(moduleDefinition.ImportReference(typeof(UIModelPropertyAttribute).GetConstructor(new Type[] { typeof(string), typeof(string), typeof(Type), typeof(PropertyComponentTargetType) })));
            var bindingType = moduleDefinition.ImportReference(typeof(UIValueNodeBufferBinding<,>)).MakeGenericInstanceType(type, moduleDefinition.TypeSystem.String);
            bindingAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, model.name));
            bindingAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, property.name));
            bindingAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.TypedReference, bindingType));
            bindingAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.Byte, (byte)PropertyComponentTargetType.BindingNode));
            assemblyDefinition.CustomAttributes.Add(bindingAttribute);

            var genericTypeAttribute = new CustomAttribute(moduleDefinition.ImportReference(typeof(RegisterGenericComponentTypeAttribute).GetConstructor(new Type[] { typeof(Type) })));
            genericTypeAttribute.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.TypedReference, bindingType));
            assemblyDefinition.CustomAttributes.Add(genericTypeAttribute);

            return type;
        }
    }

}
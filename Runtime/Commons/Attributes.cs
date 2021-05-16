using System;
using System.Runtime.CompilerServices;
using Unity.Rendering;

[assembly: InternalsVisibleTo("Unity.NeroWeNeed.UIECS.Compiler")]
namespace NeroWeNeed.UIECS
{
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class PropertyBlockAttribute : Attribute
    {
        public string name;
        public bool global;
        public PropertyBlockAttribute(string name = null, bool global = false)
        {
            this.name = name;
            this.global = global;
        }
    }
    public enum GroupElementType : byte
    {
        None, Image
    }
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class GroupElementAttribute : Attribute
    {
        public GroupElementType type;
        public GroupElementAttribute(GroupElementType type)
        {
            this.type = type;
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class PropertyAttribute : Attribute
    {
        public string name;
        public PropertyAttribute(string name)
        {
            this.name = name;
        }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class PropertyParserAttribute : Attribute
    {
        public Type propertyType;
        public Type parserContainerType;
        public string parserMethodName;
        public PropertyParserAttribute(Type propertyType, Type parserContainerType, string parserMethodName)
        {
            this.propertyType = propertyType;
            this.parserContainerType = parserContainerType;
            this.parserMethodName = parserMethodName;
        }
        public PropertyParserAttribute(Type propertyType)
        {
            this.propertyType = propertyType;
            this.parserContainerType = propertyType;
            this.parserMethodName = "TryParse";
        }
    }
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class DefaultValueAttribute : Attribute
    {
        public string value;
        public int bitOffset;
        public DefaultValueAttribute(string value, int bitOffset = -1)
        {
            this.value = value;
            this.bitOffset = bitOffset;
        }
        public DefaultValueAttribute(byte value) : this(value.ToString()) { }
        public DefaultValueAttribute(ushort value) : this(value.ToString()) { }
        public DefaultValueAttribute(uint value) : this(value.ToString()) { }
        public DefaultValueAttribute(ulong value) : this(value.ToString()) { }
        public DefaultValueAttribute(sbyte value) : this(value.ToString()) { }
        public DefaultValueAttribute(short value) : this(value.ToString()) { }
        public DefaultValueAttribute(int value) : this(value.ToString()) { }
        public DefaultValueAttribute(long value) : this(value.ToString()) { }
        public DefaultValueAttribute(float value) : this(value.ToString()) { }
        public DefaultValueAttribute(bool value, int bitOffset = 0) : this(value.ToString(), bitOffset) { }
        public DefaultValueAttribute(double value) : this(value.ToString()) { }
    }
    [Flags]
    public enum ReferencePropertyTarget : byte
    {
        None = 0,
        Self = 1,
        Parent = 2,
        Root = 4
    }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ReferencePropertyAttribute : Attribute
    {
        public string Name { get; set; }
        public ReferencePropertyTarget Target { get; set; }

        public ReferencePropertyAttribute(string name, ReferencePropertyTarget target = ReferencePropertyTarget.Self)
        {
            Name = name;
            Target = target;
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class IgnorePropertyAttribute : Attribute
    {

    }
    [AttributeUsage(AttributeTargets.Interface)]
    internal sealed class UIWrapperFunctionAttribute : Attribute
    {
        public Type @delegate;

        public UIWrapperFunctionAttribute(Type @delegate)
        {
            this.@delegate = @delegate;
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UIMaterialPropertyAttribute : Attribute
    {

        public bool AutoGenerate { get; set; }
        public NormalizerHint[] NormalizerHints { get; set; }
        public UIMaterialPropertyAttribute(params NormalizerHint[] hints)
        {
            NormalizerHints = hints;
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UIMaterialPropertyComponentAttribute : Attribute
    {
        public string Name { get; set; }
        public UIMaterialPropertyComponentAttribute(string name)
        {
            Name = name;
        }
    }
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class UIMaterialAttribute : Attribute
    {
        internal string location;
        public string Guid { set => location = $"guid:{value}"; }
        public string Path { set => location = value; }
    }

    public enum NormalizerHint : byte
    {
        None,
        Color,
        Width,
        Height
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class UIModelPropertyAttribute : Attribute
    {
        public string modelName;
        public string propertyName;
        public Type target;
        public PropertyComponentTargetType targetType;
        public UIModelPropertyAttribute(string modelName, string propertyName, Type target, PropertyComponentTargetType targetType)
        {
            this.modelName = modelName;
            this.propertyName = propertyName;
            this.target = target;
            this.targetType = targetType;
        }
    }
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class UIExtraDataTypeAttribute : Attribute
    {
        public Type type;

        public UIExtraDataTypeAttribute(Type type)
        {
            this.type = type;
        }
    }
    public enum PropertyComponentTargetType : byte
    {
        Model, BindingNode, BindingNodeChildren
    }

}
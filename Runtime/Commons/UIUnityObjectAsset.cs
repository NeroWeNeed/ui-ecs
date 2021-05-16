using System;

namespace NeroWeNeed.UIECS
{
    public unsafe struct UIUnityObjectAsset
    {
        private fixed uint values[4];

        public bool IsCreated { get => !(values[0] == 0 && values[1] == 0 && values[2] == 0 && values[3] == 0); }
    }
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class UIUnityObjectAssetAttribute : Attribute
    {
        public Type type;

        public UIUnityObjectAssetAttribute(Type type)
        {
            this.type = type;
        }
    }
}
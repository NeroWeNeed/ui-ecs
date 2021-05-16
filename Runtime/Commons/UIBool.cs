using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NeroWeNeed.UIECS
{
    public interface IUIBitProperty {}
    public interface IUIBool : IUIBitProperty { }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UIBoolAttribute : Attribute
    {
        public string[] names;
        public UIBoolAttribute(params string[] names)
        {
            this.names = names;
        }
    }
    public struct UIBool8 : IUIBool
    {
        public byte value;
    }
    public struct UIBool16 : IUIBool
    {
        public ushort value;
    }
    public struct UIBool32 : IUIBool
    {
        public uint value;
    }
    public struct UIBool64 : IUIBool
    {
        public ulong value;
    }
    public unsafe static class UIBoolExtensions {
        public static bool Get<TBool>(this TBool self,int index) where TBool : unmanaged, IUIBool {
            if (index >= 0 && index < UnsafeUtility.SizeOf<TBool>()*8)
            {
                var addr = (byte*)((&self) + (index / 8));
                return ((*addr) & (1 << (index % 8))) != 0;
            }
            else
            {
                return false;
            }
        }
        public static void Set<TBool>(this TBool self, int index,bool state) where TBool : unmanaged, IUIBool
        {
            if (index >= 0 && index < UnsafeUtility.SizeOf<TBool>()*8)
            {
                var addr = (byte*)((&self)+(index/8));
                if (state)
                {
                    *addr |= (byte)(1 << (index % 8));
                }
                else
                {
                    *addr &= (byte)~(1 << (index % 8));
                }
            }
        }
    }
}
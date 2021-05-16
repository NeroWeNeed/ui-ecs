using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace NeroWeNeed.UIECS
{
    public enum UIHorizontalAlignment : byte
    {
        Left = 0,
        Center = 0b00000001,
        Right = 0b00000010
    }
    public enum UIVerticalAlignment : byte
    {
        Top = 0,
        Center = 0b00010000,
        Bottom = 0b00100000
    }
    public enum UIAlignment : byte
    {
        TopLeft = UIHorizontalAlignment.Left | UIVerticalAlignment.Top,
        TopCenter = UIHorizontalAlignment.Center | UIVerticalAlignment.Top,
        TopRight = UIHorizontalAlignment.Right | UIVerticalAlignment.Top,
        CenterLeft = UIHorizontalAlignment.Left | UIVerticalAlignment.Center,
        Center = UIHorizontalAlignment.Center | UIVerticalAlignment.Center,
        CenterRight = UIHorizontalAlignment.Right | UIVerticalAlignment.Center,
        BottomLeft = UIHorizontalAlignment.Left | UIVerticalAlignment.Bottom,
        BottomCenter = UIHorizontalAlignment.Center | UIVerticalAlignment.Bottom,
        BottomRight = UIHorizontalAlignment.Right | UIVerticalAlignment.Bottom,
    }
    [BurstCompile]
    public static class UIAlignmentExtensions
    {
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIAlignment Merge(this UIHorizontalAlignment horizontalAlignment, UIVerticalAlignment verticalAlignment) => (UIAlignment)(((byte)horizontalAlignment) | ((byte)verticalAlignment));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static UIAlignment Merge(this UIVerticalAlignment verticalAlignment, UIHorizontalAlignment horizontalAlignment) => (UIAlignment)(((byte)horizontalAlignment) | ((byte)verticalAlignment));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static UIHorizontalAlignment GetHorizontalAlignment(this UIAlignment alignment) => (UIHorizontalAlignment)(((byte)alignment) & 0b00001111);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static UIVerticalAlignment GetVerticalAlignment(this UIAlignment alignment) => (UIVerticalAlignment)(((byte)alignment) & 0b11110000);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float GetOffset(this UIHorizontalAlignment alignment, float containerSize, float objectSize)
        {
            var multiplier = (alignment == UIHorizontalAlignment.Left) ? 0 : ((alignment == UIHorizontalAlignment.Center) ? 0.5f : 1f);
            var space = containerSize - objectSize;
            return space * multiplier;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float GetOffset(this UIVerticalAlignment alignment, float containerSize, float objectSize)
        {
            var multiplier = (alignment == UIVerticalAlignment.Top) ? 0 : ((alignment == UIVerticalAlignment.Center) ? 0.5f : 1f);
            var space = containerSize - objectSize;
            return space * multiplier;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstDiscard]
        public static float2 GetOffset(this UIAlignment alignment, float2 containerSize, float2 objectSize)
        {
            return new float2(alignment.GetHorizontalAlignment().GetOffset(containerSize.x, objectSize.x), alignment.GetVerticalAlignment().GetOffset(containerSize.y, objectSize.y));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static void GetOffset(this UIAlignment alignment,in float2 containerSize,in float2 objectSize,out float2 offsets)
        {
            offsets = new float2(alignment.GetHorizontalAlignment().GetOffset(containerSize.x, objectSize.x), alignment.GetVerticalAlignment().GetOffset(containerSize.y, objectSize.y));
        }
    }
}
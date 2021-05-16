using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NeroWeNeed.UIECS {

    public unsafe static class UIElementExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBoundedX(this float4 self) => !float.IsInfinity(self.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBoundedY(this float4 self) => !float.IsInfinity(self.w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBounded(this float4 self) => !float.IsInfinity(self.z) && !float.IsInfinity(self.w);
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GenerateBoxMesh<TElement>(this TElement element, in float4 layout, UIVertexData* vertexData, ref PropertyBlockAccessor<BoxConfigBlock> box, ref PropertyBlockAccessor<BackgroundConfigBlock> background) where TElement : struct, IUIElement {
            UnsafeUtility.WriteArrayElement<UIVertexData>(vertexData, 0, new UIVertexData(
                position: new float2(
                    x: layout.x + box.value->margin.X.realValue,
                    y: layout.y + box.value->margin.Y.realValue
                ),
                background: new float2(
                    x: background.value->image.uvData.x,
                    y: background.value->image.uvData.y
                ),
                color: background.value->color
            ));
            UnsafeUtility.WriteArrayElement<UIVertexData>(vertexData, 1, new UIVertexData(
                position: new float2(
                    x: layout.z - box.value->margin.Z.realValue,
                    y: layout.y + box.value->margin.Y.realValue
                ),
                background: new float2(
                    x: background.value->image.uvData.z,
                    y: background.value->image.uvData.y
                ),
                color: background.value->color
            ));
            UnsafeUtility.WriteArrayElement<UIVertexData>(vertexData, 2, new UIVertexData(
                position: new float2(
                    x: layout.x + box.value->margin.X.realValue,
                    y: layout.w - box.value->margin.W.realValue
                ),
                background: new float2(
                    x: background.value->image.uvData.x,
                    y: background.value->image.uvData.w
                ),
                color: background.value->color
            ));
            UnsafeUtility.WriteArrayElement<UIVertexData>(vertexData, 3, new UIVertexData(
                position: new float2(
                    x: layout.z - box.value->margin.Z.realValue,
                    y: layout.w - box.value->margin.W.realValue
                ),
                background: new float2(
                    x: background.value->image.uvData.z,
                    y: background.value->image.uvData.w
                ),
                color: background.value->color
            ));
        }
    }
}

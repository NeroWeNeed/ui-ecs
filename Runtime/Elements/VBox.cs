using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
namespace NeroWeNeed.UIECS.Elements {
    

    public struct VBox : IUIElement {
        public PropertyBlockAccessor<BorderConfigBlock> borderAccessor;
        public PropertyBlockAccessor<BoxConfigBlock> boxAccessor;
        public PropertyBlockAccessor<LayoutBoxConfigBlock> layoutBoxAccessor;
        public PropertyBlockAccessor<BackgroundConfigBlock> backgroundAccessor;
        public PropertyBlockAccessor<SizeConfigBlock> sizeAccessor;
        public ConstraintAccessor constraintsAccessor;
        public unsafe void Constrain(UIConfigHandle configHandle, UIConfigHandle* children, int childIndex, int totalChildren, out float4 childConstraints, UIExtraDataHandle extraData, long extraDataLength) {
            childConstraints = new float4(0, 0, constraintsAccessor.value->z, constraintsAccessor.value->w);
            childConstraints.z -= boxAccessor.value->margin.X.realValue + boxAccessor.value->margin.Z.realValue + boxAccessor.value->padding.X.realValue + boxAccessor.value->margin.Z.realValue + borderAccessor.value->width.X.realValue + borderAccessor.value->width.Z.realValue;
            childConstraints.w -= boxAccessor.value->margin.Y.realValue + boxAccessor.value->margin.W.realValue + boxAccessor.value->padding.Y.realValue + boxAccessor.value->margin.W.realValue + borderAccessor.value->width.Y.realValue + borderAccessor.value->width.W.realValue;
            if (childConstraints.z < 0) {
                childConstraints.z = 0;
            }
            if (childConstraints.w < 0) {
                childConstraints.w = 0;
            }
            for (int i = 0; i < childIndex; i++) {
                UnsafeUtility.ReadArrayElement<UIConfigHandle>(children, i).GetSize(out var childSize);
                childConstraints.w = math.max(0, childConstraints.w - (childSize.y + layoutBoxAccessor.value->spacing.realValue));
            }
        }
        public unsafe void Layout(UIConfigHandle configHandle, UIConfigHandle* children, int totalChildren, float2* positions, UIExtraDataHandle extraData, long extraDataLength) {
            float2 start = new float2(
                        boxAccessor.value->margin.X.realValue + boxAccessor.value->margin.Z.realValue + boxAccessor.value->padding.X.realValue + boxAccessor.value->margin.Z.realValue + borderAccessor.value->width.X.realValue + borderAccessor.value->width.Z.realValue,
                        boxAccessor.value->margin.Y.realValue + boxAccessor.value->margin.W.realValue + boxAccessor.value->padding.Y.realValue + boxAccessor.value->margin.W.realValue + borderAccessor.value->width.Y.realValue + borderAccessor.value->width.W.realValue
                        );
            for (int i = 0; i < totalChildren; i++) {
                UnsafeUtility.ReadArrayElement<UIConfigHandle>(children, i).GetSize(out var childSize);
                UnsafeUtility.WriteArrayElement<float2>(positions, i, start);
                start.y += childSize.y + layoutBoxAccessor.value->spacing.realValue;
            }
        }
        public unsafe void GenerateMeshData(UIConfigHandle configHandle, in float4 layout, UIVertexData* vertexData, UIExtraDataHandle extraData, long extraDataLength) {
            this.GenerateBoxMesh(in layout, vertexData, ref boxAccessor, ref backgroundAccessor);
        }
        public unsafe void Size(UIConfigHandle configHandle, UIConfigHandle* children, int totalChildren, out float2 size, UIExtraDataHandle extraData, long extraDataLength) {
            size = new float2(
            boxAccessor.value->margin.X.realValue + boxAccessor.value->margin.Z.realValue + boxAccessor.value->padding.X.realValue + boxAccessor.value->margin.Z.realValue + borderAccessor.value->width.X.realValue + borderAccessor.value->width.Z.realValue,
            boxAccessor.value->margin.Y.realValue + boxAccessor.value->margin.W.realValue + boxAccessor.value->padding.Y.realValue + boxAccessor.value->margin.W.realValue + borderAccessor.value->width.Y.realValue + borderAccessor.value->width.W.realValue
            );
            var contentSize = float2.zero;
            for (int i = 0; i < totalChildren; i++) {
                UnsafeUtility.ReadArrayElement<UIConfigHandle>(children, i).GetSize(out var childSize);
                contentSize.x = math.max(childSize.x, contentSize.x);
                contentSize.y += childSize.y + layoutBoxAccessor.value->spacing.realValue;
            }
            contentSize.x = math.clamp(contentSize.x, sizeAccessor.value->width.X.realValue, sizeAccessor.value->width.Y.realValue);
            contentSize.y = math.clamp(contentSize.y, sizeAccessor.value->height.X.realValue, sizeAccessor.value->height.Y.realValue);
            size += contentSize;
        }
    }
}
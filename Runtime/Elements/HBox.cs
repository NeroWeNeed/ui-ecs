using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace NeroWeNeed.UIECS.Elements
{

    public struct HBox : IUIElement
    {
        public PropertyBlockAccessor<BorderConfigBlock> borderAccessor;
        public PropertyBlockAccessor<BoxConfigBlock> boxAccessor;
        public PropertyBlockAccessor<LayoutBoxConfigBlock> layoutBoxAccessor;
        public PropertyBlockAccessor<BackgroundConfigBlock> backgroundAccessor;
        public PropertyBlockAccessor<SizeConfigBlock> sizeAccessor;
        public ConstraintAccessor constraints;
        public unsafe void Constrain(UIConfigHandle configHandle, UIConfigHandle* children, int childIndex, int totalChildren, out float4 childConstraints, UIExtraDataHandle extraData, long extraDataLength)
        {
            childConstraints = new float4(0, 0, constraints.value->z, constraints.value->w);
            if (!float.IsPositiveInfinity(constraints.value->z))
            {
                childConstraints.z -= boxAccessor.value->margin.X.realValue + boxAccessor.value->margin.Z.realValue + boxAccessor.value->padding.X.realValue + boxAccessor.value->padding.Z.realValue + borderAccessor.value->width.X.realValue + borderAccessor.value->width.Z.realValue;
                childConstraints.w -= boxAccessor.value->margin.Y.realValue + boxAccessor.value->margin.W.realValue + boxAccessor.value->padding.Y.realValue + boxAccessor.value->padding.W.realValue + borderAccessor.value->width.Y.realValue + borderAccessor.value->width.W.realValue;
                for (int i = 0; i < childIndex; i++)
                {
                    UnsafeUtility.ReadArrayElement<UIConfigHandle>(children, i).GetSize(out var childSize);
                    childConstraints.z = math.max(0, childConstraints.z - (childSize.x + layoutBoxAccessor.value->spacing.realValue));
                }
                if (childConstraints.z < 0)
                {
                    childConstraints.z = 0;
                }
                if (childConstraints.w < 0)
                {
                    childConstraints.w = 0;
                }
            }
        }
        public unsafe void Layout(UIConfigHandle configHandle, UIConfigHandle* children, int totalChildren, float2* positions, UIExtraDataHandle extraData, long extraDataLength)
        {
            float2 start = new float2(
                        boxAccessor.value->margin.X.realValue + boxAccessor.value->padding.X.realValue + borderAccessor.value->width.X.realValue,
                        boxAccessor.value->margin.Y.realValue + boxAccessor.value->padding.Y.realValue + borderAccessor.value->width.Y.realValue
                        );
            //float2 start = float2.zero;
            for (int i = 0; i < totalChildren; i++)
            {
                UnsafeUtility.ReadArrayElement<UIConfigHandle>(children, i).GetSize(out var childSize);
                UnsafeUtility.WriteArrayElement<float2>(positions, i, start);
                start.x += childSize.x + layoutBoxAccessor.value->spacing.realValue;
            }
        }
        public unsafe void GenerateMeshData(UIConfigHandle configHandle, in float4 layout, UIVertexData* vertexData, UIExtraDataHandle extraData, long extraDataLength)
        {
            this.GenerateBoxMesh(in layout, vertexData, ref boxAccessor, ref backgroundAccessor);
        }
        public unsafe void Size(UIConfigHandle configHandle, UIConfigHandle* children, int totalChildren, out float2 size, UIExtraDataHandle extraData, long extraDataLength)
        {
            size = new float2(
            boxAccessor.value->margin.X.realValue + boxAccessor.value->margin.Z.realValue + boxAccessor.value->padding.X.realValue + boxAccessor.value->padding.Z.realValue + borderAccessor.value->width.X.realValue + borderAccessor.value->width.Z.realValue,
            boxAccessor.value->margin.Y.realValue + boxAccessor.value->margin.W.realValue + boxAccessor.value->padding.Y.realValue + boxAccessor.value->padding.W.realValue + borderAccessor.value->width.Y.realValue + borderAccessor.value->width.W.realValue
            );
            //size = float2.zero;
            var contentSize = float2.zero;
            for (int i = 0; i < totalChildren; i++)
            {
                UnsafeUtility.ReadArrayElement<UIConfigHandle>(children, i).GetSize(out var childSize);

                contentSize.x += childSize.x + (layoutBoxAccessor.value->spacing.realValue * ((i + 1) < totalChildren ? 1 : 0));
                contentSize.y = math.max(childSize.y, contentSize.y);
            }
            contentSize.x = math.clamp(contentSize.x, sizeAccessor.value->width.X.realValue, sizeAccessor.value->width.Y.realValue);
            contentSize.y = math.clamp(contentSize.y, sizeAccessor.value->height.X.realValue, sizeAccessor.value->height.Y.realValue);
            size += contentSize;
        }
    }

}
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeroWeNeed.UIECS
{

    public struct UIVertexData
    {
        public static readonly float2 flipVector = new float2(1, -1);
        public static NativeArray<VertexAttributeDescriptor> AllocateVertexDescriptor(Allocator allocator = Allocator.TempJob)
        {
            var array = new NativeArray<VertexAttributeDescriptor>(5, allocator);
            array[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
            array[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);
            array[2] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4);
            array[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4);
            array[4] = new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4);
            return array;
        }

        /// <summary>
        /// 2D Coordinates (X,Y)
        /// </summary>
        public float3 position;
        public float3 normal;
        public Color32 color;
        public float4 uv1;
        public float4 uv2;

        public UIVertexData(float2 position = default, Color32 color = default, float2 background = default, float2 foreground = default)
        {
            this.position = new float3(position * flipVector, 0f);
            this.normal = math.forward();
            this.color = color;
            this.uv1 = new float4(background,foreground);
            this.uv2 = float4.zero;
            /* this.background = background;
            this.foreground = foreground; */
        }


    }
}
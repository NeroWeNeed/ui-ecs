using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace NeroWeNeed.UIECS {
    public enum BindingType {
        Color32,
        Color,
        UILength,
        CompositeData2UILength,
        CompositeData3UILength,
        CompositeData4UILength,
        CompositeData4Color,
        CompositeData4Color32
    }
    [BurstCompile]
    public unsafe static class PropertyTypeConverters {
        [BurstCompile]
        public static void Color32(Color32* color, float4* result, int length) {
            for (int i = 0; i < length; i++) {
                var value = UnsafeUtility.ReadArrayElement<Color32>(color, i);
                UnsafeUtility.WriteArrayElement(result, i, new float4(value.r / 255f, value.g / 255f, value.b / 255f, value.a / 255f));
            }
        }
        [BurstCompile]
        public static void Color(Color* color, float4* result, int length) {
            for (int i = 0; i < length; i++) {
                var value = UnsafeUtility.ReadArrayElement<Color>(color, i);
                UnsafeUtility.WriteArrayElement(result, i, new float4(value.r, value.g, value.b, value.a));
            }
        }
        [BurstCompile]
        public static void UILength(UILength* uiLength, float* result, int length) {
            for (int i = 0; i < length; i++) {
                var value = UnsafeUtility.ReadArrayElement<UILength>(uiLength, i);
                UnsafeUtility.WriteArrayElement(result, i, value.realValue);
            }
        }
        [BurstCompile]
        public static void CompositeData2UILength(CompositeData2<UILength>* uiLengths, float2* result, int length) {
            for (int i = 0; i < length; i++) {
                var value = UnsafeUtility.ReadArrayElement<CompositeData2<UILength>>(uiLengths, i);
                UnsafeUtility.WriteArrayElement(result, i, new float2(value.X.realValue, value.Y.realValue));
            }
        }
        [BurstCompile]
        public static void CompositeData3UILength(CompositeData3<UILength>* uiLengths, float3* result, int length) {
            for (int i = 0; i < length; i++) {
                var value = UnsafeUtility.ReadArrayElement<CompositeData3<UILength>>(uiLengths, i);
                UnsafeUtility.WriteArrayElement(result, i, new float3(value.X.realValue, value.Y.realValue, value.Z.realValue));
            }
        }
        [BurstCompile]
        public static void CompositeData4UILength(CompositeData4<UILength>* uiLengths, float4* result, int length) {
            for (int i = 0; i < length; i++) {
                var value = UnsafeUtility.ReadArrayElement<CompositeData4<UILength>>(uiLengths, i);
                UnsafeUtility.WriteArrayElement(result, i, new float4(value.X.realValue, value.Y.realValue, value.Z.realValue, value.W.realValue));
            }
        }
        [BurstCompile]
        public static void CompositeData4Color(CompositeData4<Color>* colors, float4x4* result, int length) {
            for (int i = 0; i < length; i++) {
                var value = UnsafeUtility.ReadArrayElement<CompositeData4<Color>>(colors, i);
                UnsafeUtility.WriteArrayElement(result, i, new float4x4(
                    new float4(value.X.r, value.X.g, value.X.b, value.X.a),
                    new float4(value.Y.r, value.Y.g, value.Y.b, value.Y.a),
                    new float4(value.Z.r, value.Z.g, value.Z.b, value.Z.a),
                    new float4(value.W.r, value.W.g, value.W.b, value.W.a)
                ));
            }
        }
        [BurstCompile]
        public static void CompositeData4Color32(CompositeData4<Color32>* colors, float4x4* result, int length) {
            for (int i = 0; i < length; i++) {
                var value = UnsafeUtility.ReadArrayElement<CompositeData4<Color32>>(colors, i);
                UnsafeUtility.WriteArrayElement(result, i, new float4x4(
                    new float4(value.X.r / 255f, value.X.g / 255f, value.X.b / 255f, value.X.a / 255f),
                    new float4(value.Y.r / 255f, value.Y.g / 255f, value.Y.b / 255f, value.Y.a / 255f),
                    new float4(value.Z.r / 255f, value.Z.g / 255f, value.Z.b / 255f, value.Z.a / 255f),
                    new float4(value.W.r / 255f, value.W.g / 255f, value.W.b / 255f, value.W.a / 255f)
                ));
            }
        }
    }
}
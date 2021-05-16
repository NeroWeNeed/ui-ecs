using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace NeroWeNeed.UIECS.Elements
{
    [UIExtraDataType(typeof(UTF8String))]
    public struct Text : IUITerminalElement
    {
        public PropertyBlockAccessor<TextConfigBlock> text;
        public PropertyBlockAccessor<FontConfigBlock> font;
        public unsafe void GenerateMeshData(UIConfigHandle configHandle, in float4 layout, UIVertexData* vertexData, UIExtraDataHandle extraData, long extraDataLength)
        {
            if (font.value->fontInfo.IsCreated)
            {
                //bool isEmpty = true;
                ref var fontInfo = ref font.value->fontInfo.Value;
                var fontHeight = font.value->size.realValue;
                var fontScale = fontHeight / fontInfo.faceInfo.lineHeight * fontInfo.faceInfo.scale;
                float2 offset = layout.xy;
                var iter = new UTF8CodePointEnumerator(((byte*)extraData.value) + text.value->text.extraDataOffset);
                int index = 0;
                while (iter.MoveNext())
                {
                    var charInfo = fontInfo[iter.Current];
                    UnsafeUtility.WriteArrayElement(vertexData, index * 4, new UIVertexData(
                    position: new float2(
                        x: offset.x + (charInfo.horizontalBearing.x * fontScale),
                        y: offset.y + ((fontInfo.faceInfo.ascentLine - charInfo.horizontalBearing.y) * fontScale)
                    ),
                    background: charInfo.uvs.xy
                    ));
                    UnsafeUtility.WriteArrayElement(vertexData, index * 4 + 1, new UIVertexData(
                    position: new float2(
                        x: offset.x + ((charInfo.horizontalBearing.x + charInfo.size.x) * fontScale),
                        y: offset.y + ((fontInfo.faceInfo.ascentLine - charInfo.horizontalBearing.y) * fontScale)
                    ),
                    background: charInfo.uvs.xy
                    ));
                    UnsafeUtility.WriteArrayElement(vertexData, index * 4 + 2, new UIVertexData(
                    position: new float2(
                        x: offset.x + (charInfo.horizontalBearing.x * fontScale),
                        y: offset.y + ((fontInfo.faceInfo.ascentLine - charInfo.horizontalBearing.y + charInfo.size.y) * fontScale)
                    ),
                    background: charInfo.uvs.xy
                    ));
                    UnsafeUtility.WriteArrayElement(vertexData, index * 4 + 3, new UIVertexData(
                    position: new float2(
                        x: offset.x + ((charInfo.horizontalBearing.x + charInfo.size.x) * fontScale),
                        y: offset.y + ((fontInfo.faceInfo.ascentLine - charInfo.horizontalBearing.y + charInfo.size.y) * fontScale)
                    ),
                    background: charInfo.uvs.xy
                    ));
                    //isEmpty = false;
                    index++;
                    offset.x += (charInfo.horizontalBearing.x + charInfo.horizontalAdvance) * fontScale;
                }

            }
        }

        public unsafe void Size(UIConfigHandle configHandle, UIConfigHandle* children, int totalChildren, out float2 size, UIExtraDataHandle extraData, long extraDataLength)
        {
            if (font.value->fontInfo.IsCreated)
            {
                ref var fontInfo = ref font.value->fontInfo.Value;
                var fontHeight = font.value->size.realValue;
                var fontScale = fontHeight / fontInfo.faceInfo.lineHeight * fontInfo.faceInfo.scale;
                size = new float2(0, fontHeight);
                var iter = new UTF8CodePointEnumerator(((byte*)extraData.value) + text.value->text.extraDataOffset);
                while (iter.MoveNext())
                {
                    var charInfo = fontInfo[iter.Current];
                    size.x += (charInfo.horizontalBearing.x + charInfo.horizontalAdvance) * fontScale;
                }
            }
            else
            {
                size = float2.zero;
            }
        }
    }
}
using System;
using NeroWeNeed.UIECS.Jobs;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore;

namespace NeroWeNeed.UIECS
{
    public struct TextConfigBlock : IPropertyBlock
    {
        public UIText text;
    }
    [PropertyBlock("font")]
    public struct FontConfigBlock : IPropertyBlock
    {
        [UIUnityObjectAsset(typeof(TMP_FontAsset))]
        public UIUnityObjectAsset asset;
        [IgnoreProperty]
        public BlobAssetReference<UIFontInfo> fontInfo;
        public UILength size;
    }
    
    public struct UIFontInfo
    {
        public UIFaceInfo faceInfo;
        public UIGlyphMap glyphs;
        public UIAtlasInfo atlasInfo;
        public UIGlyph this[uint unicode]
        {
            get
            {
                var bucketIndex = (int)(unicode % glyphs.keys.Length);
                ref var bucket = ref glyphs.keys[bucketIndex];
                for (int i = 0; i < bucket.glyphLocation.Length; i++)
                {
                    var glyphLocation = bucket.glyphLocation[i];
                    if (glyphLocation.unicode == unicode)
                    {
                        return glyphs.values[glyphLocation.index];
                    }
                }
                return default;
            }
        }
    }
    public static class UIFontExtensions
    {
        public static BlobAssetReference<UIFontInfo> CreateBlob(this TMP_FontAsset self, ushort bucketCount = 128, Allocator allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref UIFontInfo root = ref builder.ConstructRoot<UIFontInfo>();
            root.faceInfo = new UIFaceInfo(self.faceInfo);
            root.atlasInfo = new UIAtlasInfo
            {
                size = new int2(self.atlasWidth, self.atlasHeight),
                textureCount = self.atlasTextureCount
            };
            var glyphValueArray = builder.Allocate(ref root.glyphs.values, self.glyphTable.Count);
            for (int i = 0; i < self.glyphTable.Count; i++)
            {
                glyphValueArray[i] = new UIGlyph(self.glyphTable[i]);
            }
            var glyphKeyArray = builder.Allocate(ref root.glyphs.keys, bucketCount);
            var map = new NativeMultiHashMap<ushort, UIGlyphPair>(bucketCount, Allocator.Temp);
            for (int i = 0; i < self.characterTable.Count; i++)
            {
                ushort key = (ushort)(self.characterTable[i].unicode % bucketCount);
                map.Add(key, new UIGlyphPair(self.characterTable[i].unicode, (int)self.characterTable[i].glyphIndex));
            }
            var keys = map.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < keys.Length; i++)
            {
                var output = builder.Allocate(ref glyphKeyArray[keys[i]].glyphLocation, map.CountValuesForKey(keys[i]));
                if (map.TryGetFirstValue(keys[i], out var value, out var iter))
                {
                    int index = 0;
                    do
                    {
                        output[index++] = value;
                    } while (map.TryGetNextValue(out value, ref iter));
                }
            }
            var result = builder.CreateBlobAssetReference<UIFontInfo>(allocator);
            builder.Dispose();

            return result;




        }
    }
    public struct UIGlyphMap
    {
        public BlobArray<UIGlyph> values;
        public BlobArray<UIGlyphBucket> keys;

    }
    public struct UIGlyphBucket
    {
        public BlobArray<UIGlyphPair> glyphLocation;

    }
    public struct UIGlyphPair
    {
        public uint unicode;
        public int index;

        public UIGlyphPair(uint unicode, int index)
        {
            this.unicode = unicode;
            this.index = index;
        }
    }
    public struct UIGlyph
    {
        public int4 uvs;
        public float2 size;
        public float2 horizontalBearing;
        public float horizontalAdvance;
        public float scale;
        public int atlasIndex;
        public UIGlyph(Glyph glyph)
        {
            uvs = new int4(glyph.glyphRect.x, glyph.glyphRect.y, glyph.glyphRect.x + glyph.glyphRect.width, glyph.glyphRect.y + glyph.glyphRect.height);
            size = new float2(glyph.metrics.width, glyph.metrics.height);
            horizontalBearing = new float2(glyph.metrics.horizontalBearingX, glyph.metrics.horizontalBearingY);
            horizontalAdvance = glyph.metrics.horizontalAdvance;
            scale = glyph.scale;
            atlasIndex = glyph.atlasIndex;
        }
    }
    public struct UIAtlasInfo
    {
        public int2 size;
        public int textureCount;
    }
    public struct UIFaceInfo
    {
        public float lineHeight;
        public float scale;
        public float ascentLine;
        public float capLine;
        public float meanLine;
        public float baseline;
        public float descentLine;
        public float underlineOffset;
        public float underlineThickness;
        public float strikethroughOffset;
        public float strikethroughThickness;
        public float superscriptOffset;
        public float superscriptSize;
        public float subscriptOffset;
        public float subscriptSize;
        public float tabWidth;
        public float pointSize;
        public UIFaceInfo(FaceInfo faceInfo)
        {
            lineHeight = faceInfo.lineHeight;
            scale = faceInfo.scale;
            ascentLine = faceInfo.ascentLine;
            capLine = faceInfo.capLine;
            meanLine = faceInfo.meanLine;
            baseline = faceInfo.baseline;
            descentLine = faceInfo.descentLine;
            underlineOffset = faceInfo.underlineOffset;
            underlineThickness = faceInfo.underlineThickness;
            strikethroughOffset = faceInfo.strikethroughOffset;
            strikethroughThickness = faceInfo.strikethroughThickness;
            superscriptOffset = faceInfo.superscriptOffset;
            superscriptSize = faceInfo.superscriptSize;
            subscriptOffset = faceInfo.subscriptOffset;
            subscriptSize = faceInfo.subscriptSize;
            tabWidth = faceInfo.tabWidth;
            pointSize = faceInfo.pointSize;
        }
    }
    [PropertyBlock("background")]
    public struct BackgroundConfigBlock : IPropertyBlock
    {
        public UIImage image;
        [UIMaterialProperty]
        public Color color;

    }
    [PropertyBlock("border")]
    public struct BorderConfigBlock : IPropertyBlock
    {
        [CompositeName("top", "right", "bottom", "left")]
        [DefaultValue("0px 0px 0px 0px")]
        [UIMaterialProperty(NormalizerHint.Height, NormalizerHint.Width, NormalizerHint.Height, NormalizerHint.Width)]
        public CompositeData4<UILength> width;
        [CompositeName("top-left", "top-right", "bottom-right", "bottom-left")]
        [DefaultValue("0px 0px 0px 0px")]
        //[UIMaterialProperty("_BorderRadius")]
        public CompositeData4<UILength> radius;
        [CompositeName("top", "right", "bottom", "left")]
        [UIMaterialProperty]
        public CompositeData4<Color> color;

        [IgnoreProperty]
        //[UIMaterialProperty("_BorderRadiusFactor")]
        public float radiusFactor;
    }

    [PropertyBlock]
    public struct LayoutBoxConfigBlock : IPropertyBlock
    {
        [DefaultValue("0px")]
        public UILength spacing;
        [DefaultValue(nameof(UIAlignment.TopLeft))]
        public UIAlignment alignment;


    }
    [PropertyBlock]
    public struct BoxConfigBlock : IPropertyBlock
    {
        [CompositeName("top", "right", "bottom", "left", Prefix = false)]
        [DefaultValue("0px 0px 0px 0px")]
        public CompositeData4<UILength> margin;
        [CompositeName("top", "right", "bottom", "left", Prefix = false)]
        [DefaultValue("0px 0px 0px 0px")]
        public CompositeData4<UILength> padding;

    }
    [PropertyBlock]
    public struct SizeConfigBlock : IPropertyBlock
    {
        [CompositeName("min", "max")]
        [DefaultValue("0px Infinity")]
        public CompositeData2<UILength> width;
        [CompositeName("min", "max")]
        [DefaultValue("0px Infinity")]
        public CompositeData2<UILength> height;
    }
}
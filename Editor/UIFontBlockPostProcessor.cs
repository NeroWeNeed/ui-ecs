using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace NeroWeNeed.UIECS.Editor
{
    [UIProcessor(typeof(FontConfigBlock))]
    public class UIFontBlocKPostprocessor : IPropertyBlockPostprocessor
    {
        public unsafe void Postprocess(UIModelConfigPropertyBlockHeader* header, void* config, int length, PropertyBlockProcessorContext context)
        {
            FontConfigBlock* fontConfig = (FontConfigBlock*)config;
            if (fontConfig->asset.IsCreated)
            {
                var guid = UnsafeUtility.AsRef<GUID>(UnsafeUtility.AddressOf(ref fontConfig->asset));
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid.ToString()));
                var fontTypeHash = TypeHash.CalculateStableTypeHash(typeof(TMP_FontAsset));
                var upper = (uint)(fontTypeHash >> 32);
                var lower = (uint)(fontTypeHash & uint.MaxValue);
                var hash = new Unity.Entities.Hash128((uint)(font?.GetHashCode() ?? 0), upper, lower, 0);
                context.AddRequest(hash, UnsafeUtility.GetFieldOffset(typeof(FontConfigBlock).GetField(nameof(FontConfigBlock.fontInfo))));
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[assembly: InternalsVisibleTo("Unity.NeroWeNeed.UIECS.Compiler")]
namespace NeroWeNeed.UIECS.Editor
{
    internal static class UIEditorUtility
    {
        [InitializeOnLoadMethod]
        private static void InitializeUIElementManager()
        {
            UIElementManager.Initialize(false);
            CompilationPipeline.compilationFinished += (obj) => UIElementManager.InitializeFunctionPointers();
        }
        public static string GetUIAssetPath(string input)
        {
            return input.StartsWith("guid:") && input.Length > 5 ? AssetDatabase.GUIDToAssetPath(input.Substring(5)) : input;
        }
        public static TAsset LoadUIAsset<TAsset>(string input) where TAsset : UnityEngine.Object => (TAsset)LoadUIAsset(input, typeof(TAsset));
        public static TAsset LoadUIAsset<TAsset>(string input, out GUID guid) where TAsset : UnityEngine.Object => (TAsset)LoadUIAsset(input, typeof(TAsset), out guid);
        public static UnityEngine.Object LoadUIAsset(string input, Type type)
        {
            return AssetDatabase.LoadAssetAtPath(GetUIAssetPath(input), type);
        }
        public static UnityEngine.Object LoadUIAsset(string input, Type type, out GUID guid)
        {
            var path = GetUIAssetPath(input);
            guid = AssetDatabase.GUIDFromAssetPath(path);
            return AssetDatabase.LoadAssetAtPath(path, type);
        }
        public static bool TryParseUIImage(string str, ParserContext context, out UIImage image)
        {
            var asset = LoadUIAsset<Texture2D>(str);
            var uv = context.group.GetUVData(asset);
            if (uv.texture == null)
            {
                image = default;
                return false;
            }
            else
            {
                image = new UIImage { uvData = uv.uv };
                return true;
            }
        }
        public static bool TryParseColor32(string str, out Color32 color32)
        {
            var result = ColorUtility.TryParseHtmlString(str, out var color);
            color32 = color;
            return result;
        }
        public unsafe static bool TryParseUIAsset(string str, ParserContext context, out UIUnityObjectAsset uiAsset)
        {
            if (UIElementManager.TryGetAssetType(context.property, out var type))
            {
                var asset = UIEditorUtility.LoadUIAsset(str, type, out GUID guid);
                if (asset != null)
                {
                    var guidAddr = UnsafeUtility.AddressOf(ref guid);
                    uiAsset = UnsafeUtility.AsRef<UIUnityObjectAsset>(guidAddr);
                    return true;
                }
            }
            uiAsset = default;
            return false;
        }
        public static bool TryParseUIText(string str, ParserContext context, out UIText text)
        {
            text = new UIText { extraDataOffset = context.extraDataStream.Length };
            context.extraDataStream.Write(str?.Length ?? 0);
            context.extraDataStream.Write(Encoding.UTF8.GetPreamble());
            if (!string.IsNullOrEmpty(str))
                context.extraDataStream.Write(Encoding.UTF8.GetBytes(str));
            context.extraDataStream.Write((byte)0);
            return true;
        }
        public static int GetPropertyBlockOffset(Type elementType, Type propertyBlockType) => GetPropertyBlockOffset(elementType.FullName, propertyBlockType.FullName);
        public static int GetPropertyBlockOffset(string elementName, string propertyBlockName)
        {
            var element = UIElementManager.GetElement(elementName);
            var blocks = GetPropertyBlocks(element);
            int offset = UnsafeUtility.SizeOf<UIRuntimeDataHeader>() + UnsafeUtility.SizeOf<UIModelConfigHeader>();
            int index = blocks.FindIndex(element => element.type.FullName == propertyBlockName);
            if (index < 0)
                return -1;
            for (int i = 0; i < index; i++)
            {
                offset += UnsafeUtility.SizeOf<UIModelConfigPropertyBlockHeader>() + UnsafeUtility.SizeOf(blocks[i].type);
            }
            return offset;
        }
        public unsafe static List<PropertyBlockInfo> GetPropertyBlocks(ulong element)
        {
            var blocks = new List<PropertyBlockInfo>();
            foreach (var propertyBlock in UIElementManager.globalPropertyBlocks)
            {
                blocks.Add(new PropertyBlockInfo(UIElementManager.GetPropertyBlock(propertyBlock), true, propertyBlock));
            }
            foreach (var propertyBlock in UIElementManager.elementPropertyBlocks.GetValuesForKey(element))
            {
                blocks.Add(new PropertyBlockInfo(UIElementManager.GetPropertyBlock(propertyBlock.blockHash), propertyBlock.IsRequired, propertyBlock.blockHash));
            }
            blocks.Sort((a, b) => a.hash.CompareTo(b.hash));
            return blocks;
        }
        internal unsafe static void HandleProperty<TPropertyValue>(string str, IntPtr configBuffer, int offset, int fieldOffset, int bitOffset, ParserContext context) where TPropertyValue : unmanaged
        {
            if ((typeof(IUIBool).IsAssignableFrom(typeof(TPropertyValue)) || (typeof(TPropertyValue) == typeof(bool))) && bitOffset >= 0)
            {
                if (UIPropertyParser.TryParse(str, context, out bool result))
                {
                    var state = (byte)(1 << (bitOffset % 8));
                    var destination = (byte*)(configBuffer + offset + fieldOffset + (bitOffset / 8));
                    byte output;
                    if (result)
                    {
                        output = (byte)(*destination | state);
                    }
                    else
                    {
                        output = (byte)(*destination & (((byte)~state)));
                    }
                    UnsafeUtility.CopyStructureToPtr(ref output, destination);
                }
                else
                {
                    Debug.LogError($"Failed to parse property value {str}");
                }
            }
            else if (UIPropertyParser.TryParse(str, context, out TPropertyValue result))
            {
                UnsafeUtility.CopyStructureToPtr(ref result, (configBuffer + (offset + fieldOffset)).ToPointer());
            }
            else
            {
                Debug.LogError($"Failed to parse property value {str}");
            }
        }
        public struct PropertyBlockInfo
        {
            public Type type;
            public bool required;
            public ulong hash;
            public PropertyBlockInfo(Type type, bool required, ulong hash)
            {
                this.type = type;
                this.required = required;
                this.hash = hash;
            }
        }
    }
}
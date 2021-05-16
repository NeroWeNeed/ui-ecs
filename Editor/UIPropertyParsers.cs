using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NeroWeNeed.Commons.Editor;
using NeroWeNeed.UIECS;
using NeroWeNeed.UIECS.Editor;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;

[assembly: PropertyParser(typeof(bool))]
[assembly: PropertyParser(typeof(char))]
[assembly: PropertyParser(typeof(byte))]
[assembly: PropertyParser(typeof(ushort))]
[assembly: PropertyParser(typeof(uint))]
[assembly: PropertyParser(typeof(ulong))]
[assembly: PropertyParser(typeof(sbyte))]
[assembly: PropertyParser(typeof(short))]
[assembly: PropertyParser(typeof(int))]
[assembly: PropertyParser(typeof(long))]
[assembly: PropertyParser(typeof(float))]
[assembly: PropertyParser(typeof(double))]
[assembly: PropertyParser(typeof(UILength))]
[assembly: PropertyParser(typeof(Color), typeof(ColorUtility), nameof(ColorUtility.TryParseHtmlString))]
[assembly: PropertyParser(typeof(Color32), typeof(UIEditorUtility), nameof(UIEditorUtility.TryParseColor32))]
[assembly: PropertyParser(typeof(UIImage), typeof(UIEditorUtility), nameof(UIEditorUtility.TryParseUIImage))]
[assembly: PropertyParser(typeof(UIUnityObjectAsset), typeof(UIEditorUtility), nameof(UIEditorUtility.TryParseUIAsset))]
[assembly: PropertyParser(typeof(UIText), typeof(UIEditorUtility), nameof(UIEditorUtility.TryParseUIText))]
namespace NeroWeNeed.UIECS.Editor
{
    public delegate bool UIPropertyParserDelegate<TValue>(string str, out TValue result);
    public delegate bool UIPropertyParserDelegateWithContext<TValue>(string str, ParserContext context, out TValue result);

    public interface IUIPropertyParser
    {
        bool TryParse(string str, ParserContext context, out object result);
    }
    public interface IUIPropertyParser<TValue> : IUIPropertyParser
    {
        bool TryParse(string str, ParserContext context, out TValue result);
    }
    public struct ParserContext
    {
        public UIGroup group;
        public UIProperty property;
        public MemoryBinaryWriter extraDataStream;
    }
    public struct PropertyBlockProcessorContext
    {
        public int nodeIndex;
        public UIGroup group;
        public ulong blockHash;
        internal List<BlobAssetRequest> blobRequests;
        public static PropertyBlockProcessorContext Create(UIGroup group, int nodeIndex, ulong block, List<BlobAssetRequest> requests)
        {
            return new PropertyBlockProcessorContext
            {
                nodeIndex = nodeIndex,
                group = group,
                blockHash = block,
                blobRequests = requests
            };
        }
        public void AddRequest(Unity.Entities.Hash128 hash, int offset)
        {
            blobRequests.Add(new BlobAssetRequest(hash, nodeIndex, blockHash, offset));
        }

    }
    public struct BlobAssetRequest
    {
        public Unity.Entities.Hash128 hash;
        public ulong block;
        public int offset;
        public int nodeIndex;

        public BlobAssetRequest(Unity.Entities.Hash128 hash, int nodeIndex, ulong block, int offset)
        {
            this.nodeIndex = nodeIndex;
            this.hash = hash;
            this.block = block;
            this.offset = offset;
        }
    }


    public static class UIPropertyParser
    {
        private static readonly Dictionary<Type, IUIPropertyParser> parsers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetCustomAttributes<PropertyParserAttribute>())
            .ToDictionary(attr => attr.propertyType, attr => (IUIPropertyParser)typeof(UIPropertyParser).GetMethod(nameof(UIPropertyParser.CreateParser), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(attr.propertyType).Invoke(null, new object[] { attr.parserContainerType, attr.parserMethodName })
            );
        public static bool TryParse(string str, Type type, out object result)
        {
            var parameters = new object[] { str, null };
            var returnResult = (bool)typeof(UIPropertyParser).GetGenericMethod(nameof(TryParse), BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(type).Invoke(null, parameters);
            result = parameters[1];
            return returnResult;
        }
        public unsafe static bool TryParse<TValue>(string str, ParserContext context, out TValue result) where TValue : unmanaged
        {
            if (typeof(ICompositeData).IsAssignableFrom(typeof(TValue)) && typeof(TValue).IsConstructedGenericType)
            {
                var inputs = str.Split(' ');
                var dataType = typeof(TValue).GenericTypeArguments[0];
                var fields = typeof(TValue).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var fieldCount = fields.Length;
                var fieldSize = Marshal.SizeOf(dataType);
                Array.Sort(fields, (a, b) => UnsafeUtility.GetFieldOffset(a).CompareTo(UnsafeUtility.GetFieldOffset(b)));
                var parser = parsers[dataType];
                TValue value = default;
                IntPtr valueAddr = (IntPtr)UnsafeUtility.AddressOf(ref value);


                object temp = default;
                if (fieldCount == 4 && inputs.Length != fieldCount)
                {
                    switch (inputs.Length)
                    {
                        case 3:
                            if (parser.TryParse(inputs[0], context, out temp))
                            {
                                Marshal.StructureToPtr(temp, valueAddr, true);

                            }
                            else
                            {
                                result = default;
                                return false;
                            }
                            if (parser.TryParse(inputs[1], context, out temp))
                            {
                                Marshal.StructureToPtr(temp, valueAddr + fieldSize, true);
                                Marshal.StructureToPtr(temp, valueAddr + (fieldSize * 2), true);
                            }
                            else
                            {
                                result = default;
                                return false;
                            }
                            if (parser.TryParse(inputs[2], context, out temp))
                            {
                                Marshal.StructureToPtr(temp, valueAddr + (fieldSize * 3), true);
                            }
                            else
                            {
                                result = default;
                                return false;
                            }
                            break;
                        case 2:
                            if (parser.TryParse(inputs[0], context, out temp))
                            {
                                Marshal.StructureToPtr(temp, valueAddr, true);
                                Marshal.StructureToPtr(temp, valueAddr + (fieldSize * 2), true);
                            }
                            else
                            {
                                result = default;
                                return false;
                            }
                            if (parser.TryParse(inputs[1], context, out temp))
                            {
                                Marshal.StructureToPtr(temp, valueAddr + fieldSize, true);
                                Marshal.StructureToPtr(temp, valueAddr + (fieldSize * 3), true);
                            }
                            else
                            {
                                result = default;
                                return false;
                            }
                            break;
                        case 1:
                            if (parser.TryParse(inputs[0], context, out temp))
                            {
                                Marshal.StructureToPtr(temp, valueAddr, true);
                                Marshal.StructureToPtr(temp, valueAddr + fieldSize, true);
                                Marshal.StructureToPtr(temp, valueAddr + (fieldSize * 2), true);
                                Marshal.StructureToPtr(temp, valueAddr + (fieldSize * 3), true);
                            }
                            else
                            {
                                result = default;
                                return false;
                            }
                            break;
                        default:
                            result = default;
                            return false;
                    }
                }
                else
                {
                    int index = 0;
                    while (index < fields.Length && index < inputs.Length)
                    {
                        if (parser.TryParse(inputs[index], context, out temp))
                        {
                            Marshal.StructureToPtr(temp, valueAddr + (fieldSize * index), true);
                        }
                        else
                        {
                            result = default;
                            return false;
                        }
                        index++;
                    }
                    while (index < fields.Length)
                    {
                        Marshal.StructureToPtr(temp, valueAddr + (fieldSize * index), true);
                        index++;
                    }
                }
                result = value;
                return true;
            }
            else if (typeof(TValue).IsEnum)
            {
                return Enum.TryParse(str, true, out result);
            }
            else
            {
                return ((IUIPropertyParser<TValue>)parsers[typeof(TValue)]).TryParse(str, context, out result);
            }
        }

        private static IUIPropertyParser CreateParser<TValue>(Type methodContainer, string name)
        {
            foreach (var methodInfo in methodContainer.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(method => method.Name == name))
            {

                var withGroupDelegate = (UIPropertyParserDelegateWithContext<TValue>)Delegate.CreateDelegate(typeof(UIPropertyParserDelegateWithContext<TValue>), methodInfo, false);

                if (withGroupDelegate != null)
                {
                    return new ParserWithContext<TValue>(withGroupDelegate);
                }
                var @delegate = (UIPropertyParserDelegate<TValue>)Delegate.CreateDelegate(typeof(UIPropertyParserDelegate<TValue>), methodInfo, false);
                if (@delegate != null)
                {
                    return new Parser<TValue>(@delegate);
                }

            }
            throw new Exception($"No Parser found for type {typeof(TValue).AssemblyQualifiedName}");


        }
        private struct ParserWithContext<TValue> : IUIPropertyParser<TValue>
        {
            public UIPropertyParserDelegateWithContext<TValue> parser;
            public ParserWithContext(UIPropertyParserDelegateWithContext<TValue> parser)
            {
                this.parser = parser;
            }
            public bool TryParse(string str, ParserContext context, out TValue result)
            {
                return parser.Invoke(str, context, out result);
            }

            public bool TryParse(string str, ParserContext context, out object result)
            {
                var r = TryParse(str, context, out TValue typedResult);
                result = typedResult;
                return r;
            }
        }
        private struct Parser<TValue> : IUIPropertyParser<TValue>
        {
            public UIPropertyParserDelegate<TValue> parser;
            public Parser(UIPropertyParserDelegate<TValue> parser)
            {
                this.parser = parser;
            }

            public bool TryParse(string str, ParserContext context, out TValue result)
            {
                return parser.Invoke(str, out result);
            }

            public bool TryParse(string str, ParserContext context, out object result)
            {
                var r = TryParse(str, context, out TValue typedResult);
                result = typedResult;
                return r;
            }
        }
    }
}
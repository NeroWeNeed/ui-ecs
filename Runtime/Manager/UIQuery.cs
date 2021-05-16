using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace NeroWeNeed.UIECS
{
    public unsafe struct UIQuery : IDisposable, IEnumerable<UIQuery.Node>
    {
        internal void* buffer;
        internal Allocator allocator;
        public int Length { get => ((Header*)buffer)->nodeCount; }

        public UIQuery(string query, Allocator allocator = Allocator.Persistent)
        {
            this.allocator = allocator;
            fixed (byte* queryString = ASCIIEncoding.Default.GetBytes(query))
            {
                buffer = UIQueryBuilder.Create(queryString, query.Length, allocator);
            }
        }
        public IEnumerator<Node> GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        public void Dispose()
        {
            UnsafeUtility.Free(buffer, allocator);
        }

        internal struct Header
        {
            public long totalSize;
            public int nodeCount;
        }
        internal struct NodeHeader
        {
            public int totalSize;
            public byte operation;
        }
        internal struct ClassHeader
        {
            public int totalSize;
            public byte count;
        }
        internal struct ContentHeader
        {
            public int totalSize;
        }
        public struct Node
        {
            public Handle element;
            public Handle name;
            public ClassHandle classes;
            public int classLength;
            public ClassHandle pseudoClasses;
            public int pseudoClassLength;
            public Operation operation;
            public struct Handle
            {
                public byte* value;
                public int length;
            }
            public struct ClassHandle : IEnumerable<Handle>
            {
                public byte* value;

                public int count;

                public IEnumerator<Handle> GetEnumerator()
                {
                    return new Enumerator(ref this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return new Enumerator(ref this);
                }

                public struct Enumerator : IEnumerator<Handle>
                {
                    internal byte* data;
                    public int Count { get; }
                    internal int offset;
                    internal int index;
                    private Handle current;
                    Handle IEnumerator<Handle>.Current { get => current; }


                    object IEnumerator.Current => current;


                    internal Enumerator(ref ClassHandle classHandle)
                    {
                        this.data = (byte*)classHandle.value;
                        this.Count = classHandle.count;
                        current = default;
                        offset = 0;
                        index = -1;
                    }

                    public void Dispose() { }

                    public bool MoveNext()
                    {
                        offset += offset == 0 ? UnsafeUtility.SizeOf<UIQuery.ClassHeader>() : (((UIQuery.ClassHeader*)(data + offset))->totalSize);
                        index++;
                        var pos = data + offset;
                        current = new Handle
                        {
                            value = pos + UnsafeUtility.SizeOf<UIQuery.ClassHeader>(),
                            length = ((ClassHeader*)pos)->totalSize
                        };
                        return index < Count;
                    }

                    public void Reset()
                    {
                        current = default;
                        offset = 0;
                        index = -1;
                    }
                }
            }
        }
        public enum Operation : byte
        {
            None = 0,
            Descendant = 0b00000011,
            DirectDescendant = 0b00000111,
            Sequential = 0b00001011,
            Ancestor = 0b00001111,
        }
        public struct Enumerator : IEnumerator<UIQuery.Node>
        {
            internal byte* data;
            public int Count { get; }
            internal int offset;
            internal int index;
            private Node current;
            Node IEnumerator<Node>.Current { get => current; }


            object IEnumerator.Current => current;


            internal Enumerator(ref UIQuery query)
            {
                this.data = (byte*)query.buffer;
                this.Count = query.Length;
                current = default;

                offset = 0;
                index = -1;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                offset += offset == 0 ? UnsafeUtility.SizeOf<UIQuery.Header>() : (((NodeHeader*)(data + offset))->totalSize + UnsafeUtility.SizeOf<UIQuery.NodeHeader>());
                index++;
                var pos = data + offset;
                var elementNameLength = *(int*)(pos + UnsafeUtility.SizeOf<UIQuery.NodeHeader>());
                var elementName = pos + UnsafeUtility.SizeOf<NodeHeader>() + sizeof(int);
                var nameLength = *(int*)(pos + elementNameLength + sizeof(int) + UnsafeUtility.SizeOf<UIQuery.NodeHeader>());
                var name = pos + elementNameLength + sizeof(int) + UnsafeUtility.SizeOf<NodeHeader>() + sizeof(int);
                current = new UIQuery.Node
                {
                    element = new Node.Handle
                    {
                        value = elementName,
                        length = elementNameLength
                    },
                    name = new Node.Handle
                    {
                        value = name,
                        length = nameLength
                    },
                    operation = (UIQuery.Operation)((UIQuery.NodeHeader*)pos)->operation
                };
                return index < Count;
            }

            public void Reset()
            {
                current = default;
                offset = 0;
                index = -1;
            }
        }

    }
    public unsafe static class UIQueryBuilder
    {
        internal static readonly TokenOperation[] operations = new TokenOperation[] {
            new TokenOperation(' ',TokenOperationType.Descendant),
            new TokenOperation('>',TokenOperationType.DirectDescendant),
            new TokenOperation('+',TokenOperationType.Sequential),
            new TokenOperation('~',TokenOperationType.Ancestor),
            new TokenOperation('#',TokenOperationType.NodeName),
            new TokenOperation('.',TokenOperationType.NodeClassName),
            new TokenOperation(':',TokenOperationType.NodePseudoClassName),
        };
        public unsafe static void* Create(byte* data, long length, Allocator allocator)
        {

            var queryData = CreateQueryData(data, length, allocator);
            return queryData;
        }
        internal static NativeList<Token> Tokenize(byte* data, long length, Allocator allocator = Allocator.Temp)
        {
            var tokens = new NativeList<Token>(allocator);
            int index = 0;
            int lastIndex = 0;
            while (index < length)
            {
                if (IsOperation(*(data + index), out var operationType))
                {
                    if (index - lastIndex > 0)
                    {
                        tokens.Add(new Token(lastIndex, index - lastIndex, TokenOperationType.Content));
                    }
                    tokens.Add(new Token(index, 1, operationType));
                    index++;
                    lastIndex = index;
                }
                index++;
            }
            if (index - lastIndex > 0)
            {
                tokens.Add(new Token(lastIndex, index - lastIndex, TokenOperationType.Content));
            }
            return tokens;
        }
        internal unsafe static void* CreateQueryData(byte* data, long length, Allocator allocator)
        {
            var tokens = Tokenize(data, length);
            var classes = new NativeMultiHashMap<int, Token>(4, Allocator.Temp);
            var pseudoClasses = new NativeMultiHashMap<int, Token>(4, Allocator.Temp);
            var nodes = new NativeList<QueryNode>(Allocator.Temp);
            QueryNode current = default;
            TokenOperationType currentOperation = TokenOperationType.NodeElementName;

            bool consumeNode = false;
            int nodeIndex = 1;
            foreach (var token in tokens)
            {
                if (token.type == TokenOperationType.Content)
                {
                    switch (currentOperation)
                    {
                        case TokenOperationType.NodeName:
                            current.nameToken = token;
                            break;
                        case TokenOperationType.NodeElementName:
                            current.elementNameToken = token;
                            break;
                        case TokenOperationType.NodeClassName:
                            classes.Add(current.nodeIndex, token);
                            break;
                        case TokenOperationType.NodePseudoClassName:
                            pseudoClasses.Add(current.nodeIndex, token);
                            break;
                        case TokenOperationType.Descendant:
                        case TokenOperationType.DirectDescendant:
                        case TokenOperationType.Sequential:
                        case TokenOperationType.Ancestor:
                            current.operation = token.type;
                            break;
                    }
                }
                else if (token.type.IsOperation())
                {
                    currentOperation = token.type;
                }
                if (consumeNode)
                {
                    nodes.Add(current);
                    currentOperation = TokenOperationType.NodeElementName;
                    current = new QueryNode { nodeIndex = nodeIndex++ };
                }
                consumeNode = token.type.ConsumesNode();
            }

            var queryData = AllocateQueryData(data, ref nodes, ref classes, ref pseudoClasses, allocator);
            classes.Dispose();
            pseudoClasses.Dispose();
            nodes.Dispose();
            tokens.Dispose();
            return queryData;

        }
        internal static void* AllocateQueryData(byte* data, ref NativeList<QueryNode> nodes, ref NativeMultiHashMap<int, Token> classes, ref NativeMultiHashMap<int, Token> pseudoClasses, Allocator allocator)
        {
            var nodeSizes = GetQuerySize(nodes, classes, pseudoClasses, Allocator.Temp, out long queryDataSize);
            var queryData = (byte*)UnsafeUtility.Malloc(queryDataSize, 0, allocator);
            var queryDataHeader = new UIQuery.Header
            {
                totalSize = queryDataSize - UnsafeUtility.SizeOf<UIQuery.Header>(),
                nodeCount = nodes.Length
            };
            UnsafeUtility.CopyStructureToPtr(ref queryDataHeader, queryData);
            int offset = UnsafeUtility.SizeOf<UIQuery.Header>();
            foreach (var node in nodes)
            {
                var header = new UIQuery.NodeHeader
                {
                    totalSize = nodeSizes[node.nodeIndex].Item1 - UnsafeUtility.SizeOf<UIQuery.NodeHeader>(),
                    operation = (byte)node.operation
                };
                var elementNameHeader = new UIQuery.ContentHeader
                {
                    totalSize = node.elementNameToken.length
                };
                var nameHeader = new UIQuery.ContentHeader
                {
                    totalSize = node.nameToken.length
                };
                var classHeader = new UIQuery.ClassHeader
                {
                    totalSize = nodeSizes[node.nodeIndex].Item2,
                    count = (byte)classes.CountValuesForKey(node.nodeIndex)
                };
                var pseudoClassHeader = new UIQuery.ClassHeader
                {
                    totalSize = nodeSizes[node.nodeIndex].Item3,
                    count = (byte)pseudoClasses.CountValuesForKey(node.nodeIndex)
                };
                UnsafeUtility.CopyStructureToPtr(ref header, queryData + offset);
                offset += UnsafeUtility.SizeOf<UIQuery.NodeHeader>();
                UnsafeUtility.CopyStructureToPtr(ref elementNameHeader, queryData + offset);
                offset += UnsafeUtility.SizeOf<UIQuery.ContentHeader>();
                UnsafeUtility.MemCpy(queryData + offset, data + node.elementNameToken.offset, elementNameHeader.totalSize);
                offset += elementNameHeader.totalSize;
                UnsafeUtility.CopyStructureToPtr(ref nameHeader, queryData + offset);
                offset += UnsafeUtility.SizeOf<UIQuery.ContentHeader>();
                UnsafeUtility.MemCpy(queryData + offset, data + node.nameToken.offset, nameHeader.totalSize);
                offset += nameHeader.totalSize;
                UnsafeUtility.CopyStructureToPtr(ref classHeader, queryData + offset);
                offset += UnsafeUtility.SizeOf<UIQuery.ClassHeader>();
                if (classes.TryGetFirstValue(node.nodeIndex, out var classToken, out var classIter))
                {
                    do
                    {
                        var classItemHeader = new UIQuery.ContentHeader
                        {
                            totalSize = classToken.length
                        };
                        UnsafeUtility.CopyStructureToPtr(ref classItemHeader, queryData + offset);
                        offset += UnsafeUtility.SizeOf<UIQuery.ContentHeader>();
                        UnsafeUtility.MemCpy(queryData + offset, data + classToken.offset, classItemHeader.totalSize);
                        offset += classItemHeader.totalSize;
                    } while (classes.TryGetNextValue(out classToken, ref classIter));
                }
                UnsafeUtility.CopyStructureToPtr(ref pseudoClassHeader, queryData + offset);
                offset += UnsafeUtility.SizeOf<UIQuery.ClassHeader>();
                if (pseudoClasses.TryGetFirstValue(node.nodeIndex, out var pseudoClassToken, out var pseduoClassIter))
                {
                    do
                    {
                        var pseudoClassItemHeader = new UIQuery.ContentHeader
                        {
                            totalSize = pseudoClassToken.length
                        };
                        UnsafeUtility.CopyStructureToPtr(ref pseudoClassItemHeader, queryData + offset);
                        offset += UnsafeUtility.SizeOf<UIQuery.ContentHeader>();
                        UnsafeUtility.MemCpy(queryData + offset, data + pseudoClassToken.offset, pseudoClassItemHeader.totalSize);
                        offset += pseudoClassItemHeader.totalSize;
                    } while (classes.TryGetNextValue(out classToken, ref classIter));
                }



            }
            return queryData;

        }
        internal static NativeArray<ValueTuple<int, int, int>> GetQuerySize(NativeList<QueryNode> nodes, NativeMultiHashMap<int, Token> classes, NativeMultiHashMap<int, Token> pseudoClasses, Allocator allocator, out long totalSize)
        {
            long size = UnsafeUtility.SizeOf<UIQuery.Header>();
            var sizes = new NativeArray<ValueTuple<int, int, int>>(nodes.Length, allocator);
            foreach (var node in nodes)
            {
                int nodeSize = UnsafeUtility.SizeOf<UIQuery.NodeHeader>()
                + UnsafeUtility.SizeOf<UIQuery.ContentHeader>() + node.elementNameToken.length
                + UnsafeUtility.SizeOf<UIQuery.ContentHeader>() + node.nameToken.length
                + UnsafeUtility.SizeOf<UIQuery.ClassHeader>()
                + UnsafeUtility.SizeOf<UIQuery.ClassHeader>();
                int classSectionSize = 0;
                int pseudoClassSectionSize = 0;
                if (classes.TryGetFirstValue(node.nodeIndex, out var @class, out var classIter))
                {
                    do
                    {
                        nodeSize += UnsafeUtility.SizeOf<UIQuery.ContentHeader>() + @class.length;
                        classSectionSize += UnsafeUtility.SizeOf<UIQuery.ContentHeader>() + @class.length;
                    } while (classes.TryGetNextValue(out @class, ref classIter));
                }
                if (pseudoClasses.TryGetFirstValue(node.nodeIndex, out var pseudoClass, out var pseudoClassIter))
                {
                    do
                    {
                        nodeSize += UnsafeUtility.SizeOf<UIQuery.ContentHeader>() + pseudoClass.length;
                        pseudoClassSectionSize += UnsafeUtility.SizeOf<UIQuery.ContentHeader>() + pseudoClass.length;
                    } while (pseudoClasses.TryGetNextValue(out pseudoClass, ref pseudoClassIter));
                }
                size += nodeSize;
                sizes[node.nodeIndex] = (nodeSize, classSectionSize, pseudoClassSectionSize);

            }
            totalSize = size;
            return sizes;

        }
        internal static bool IsOperation(byte symbol, out TokenOperationType type)
        {
            for (int i = 0; i < operations.Length; i++)
            {
                if (operations[i].symbol == symbol)
                {
                    type = operations[i].type;
                    return true;
                }
            }
            type = default;
            return false;
        }
        internal readonly struct TokenOperation
        {
            public readonly byte symbol;
            public readonly TokenOperationType type;
            public TokenOperation(char symbol, TokenOperationType type)
            {
                this.symbol = (byte)symbol;
                this.type = type;
            }
        }
        internal readonly struct Token
        {
            public readonly int offset;
            public readonly int length;
            public readonly TokenOperationType type;

            public Token(int offset, int length, TokenOperationType type)
            {
                this.offset = offset;
                this.length = length;
                this.type = type;
            }
        }
        internal struct QueryNode
        {
            public Token elementNameToken;
            public Token nameToken;
            public int nodeIndex;
            public TokenOperationType operation;
        }
        internal struct QueryDataHeader
        {
            long totalSize;
            int nodeCount;
        }
        internal struct QueryNodeDataHeader
        {
            int totalSize;
            int nodeCount;
        }
        internal enum TokenOperationType : byte
        {
            None = 0,
            Content = 0b00000100,
            NodeName = 0b00000001,
            NodeElementName = 0b00000101,
            NodeClassName = 0b00001001,
            NodePseudoClassName = 0b00001101,
            Descendant = 0b00000011,
            DirectDescendant = 0b00000111,
            Sequential = 0b00001011,
            Ancestor = 0b00001111,

        }
        internal static bool ConsumesNode(this TokenOperationType self) => (((byte)self) & 2) != 0;
        internal static bool IsOperation(this TokenOperationType self) => (((byte)self) & 1) != 0;
    }
    public enum UIQueryNodeType : byte
    {
        Class = 0,
        Element = 1,
        Name = 2,
        PseudoClass = 3
    }

}
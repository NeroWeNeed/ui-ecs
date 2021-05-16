using System;
using System.Collections.Generic;
using System.Linq;
using NeroWeNeed.Commons;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace NeroWeNeed.UIECS.Editor {
    public class UIViewAsset : ScriptableObject {
        public UIGroup group;
        public List<Node> nodes;
        public Node data;
        [SerializeField]
        internal List<UnityEngine.Object> referencedAssets;
        public int NodeCount { get => nodes?.Count ?? 0; }
        public Node this[int index] {
            get => nodes[index];
        }
        public int IndexOf(Node node) => this.nodes.IndexOf(node);
        public Node GetParentNode(Node node) => node.parentIndex < 0 ? null : nodes[node.parentIndex];
        public Node GetChildNode(Node node, int child) => node.childrenIndices == null || node.childrenIndices.Length <= child ? null : nodes[node.childrenIndices[child]];
        public IEnumerable<Node> GetChildren(Node node) => node.childrenIndices?.Select(i => nodes[i]);
        [Serializable]
        public class Node {
            public SerializableType type;
            public string name;
            public string[] classes;
            public int parentIndex;
            public int index;
            public string modelPropertyBinding;
            public int ClassCount { get => (classes?.Length) ?? 0; }
            public Property[] properties;
            public Material material;
            public int[] childrenIndices;
            public int ChildCount { get => (childrenIndices?.Length) ?? 0; }
            [Serializable]
            public struct Property {
                public string name;
                public string value;
            }
        }

    }
}
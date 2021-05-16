using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
namespace NeroWeNeed.UIECS.Editor
{
    public class UIGroup : ScriptableObject
    {
        public const string ShaderPath = "Packages/github.neroweneed.ui-ecs/Runtime/Resources/UIShader.shadergraph";
        public const string AtlasPropertyName = "_Atlas";
        public string groupName;
        public List<GroupEntry> entries = new List<GroupEntry>();
        public List<Image> images = new List<Image>();
        public List<Texture> extraImages = new List<Texture>();
        [SerializeField]
        private Texture2D atlas;
        public int2 atlasSize = new int2(1024, 1024);
        [SerializeField]
        private Material material;
        public List<ImageUVData> uvs = new List<ImageUVData>();
        public Material Material { get => material; }
        [Serializable]
        public struct ImageUVData
        {
            public Texture texture;
            public float4 uv;
        }

        [Serializable]
        public struct Image
        {
            public Texture texture;
            public UIViewAsset source;
        }

        public ImageUVData GetUVData(Texture texture) => uvs.Find(uvData => uvData.texture == texture);


        private int GetAtlasHash()
        {
            int hashCode = -1101999475;
            if (images != null)
            {
                for (int i = 0; i < images.Count; i++)
                    hashCode = hashCode * -1521134295 + images[i].texture.GetHashCode();
            }
            if (extraImages != null)
            {
                for (int i = 0; i < extraImages.Count; i++)
                    hashCode = hashCode * -1521134295 + extraImages[i].GetHashCode();
            }
            return hashCode;
        }
        public GroupEntryReference GetEntryReference(string guid)
        {
            return new GroupEntryReference(entries.FindIndex(entry => entry.source == guid));
        }
        public void UpdateEntry(GroupEntryReference entryReference, GroupEntry newEntry)
        {
            bool shouldUpdate = false;
            newEntry.images?.Sort();
            if (entryReference.ShouldAdd)
            {
                entries.Add(newEntry);
                shouldUpdate = true;
            }
            else
            {
                var oldEntry = entries[entryReference.index];
                if (!oldEntry.Equals(newEntry))
                {
                    entries[entryReference.index] = newEntry;
                    shouldUpdate = true;
                }
            }
            if (shouldUpdate)
            {
                UpdateMaterial();
                EditorUtility.SetDirty(this);
            }
        }
        internal void UpdateMaterial()
        {
            if (material == null)
            {
                material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath))
                {
                    name = "Material"
                };
                AssetDatabase.AddObjectToAsset(material, AssetDatabase.GetAssetPath(this));
                EditorUtility.SetDirty(material);
            }
            var textures = entries.SelectMany(entry => entry.images).Concat(extraImages).OfType<Texture2D>().ToArray();
            if (textures.Length > 0)
            {
                if (atlas == null)
                {
                    atlas = new Texture2D(atlasSize.x, atlasSize.y)
                    {
                        name = "Atlas"
                    };
                    AssetDatabase.AddObjectToAsset(atlas, AssetDatabase.GetAssetPath(this));
                }
                atlas.PackTextures(textures, 0, 8192);
                material.SetTexture(AtlasPropertyName, atlas);
                EditorUtility.SetDirty(atlas);
            }
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
/*         private void OnValidate()
        {
            Debug.Log("Validating...");
        } */
        internal void ClearGroupData(UIViewAsset modelAsset)
        {
            int removed = images.RemoveAll(image => image.source == modelAsset || image.source == null);
            if (removed > 0)
                EditorUtility.SetDirty(this);
        }
        [Serializable]
        public struct GroupEntry : IEquatable<GroupEntry>
        {
            public string source;
            public List<Texture> images;

            public GroupEntry(string guid, params Texture[] images)
            {
                this.source = guid;
                this.images = images.ToList();
            }
            public GroupEntry(string guid, IEnumerable<Texture> images)
            {
                this.source = guid;
                this.images = images.ToList();
            }

            public bool Equals(GroupEntry other)
            {
                return EqualityComparer<string>.Default.Equals(source, other.source) && ((images == null && other.images == null) || (images != null && other.images != null && images.SequenceEqual(other.images)));
            }
        }
        public struct GroupEntryReference
        {
            public int index;
            public bool ShouldAdd { get => index < 0; }

            public GroupEntryReference(int index)
            {
                this.index = index;
            }
        }
    }
}


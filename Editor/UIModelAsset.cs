using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Mono.Cecil;
using NeroWeNeed.Commons;
using Unity.Entities;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Compilation;
using UnityEngine;

namespace NeroWeNeed.UIECS.Editor
{
    public class UIModelAsset : ScriptableObject
    {
        public string modelName;
        [SerializeReference]
        public List<UIModelAssetBaseProperty> properties;

        public string assembly;
    }
 


}
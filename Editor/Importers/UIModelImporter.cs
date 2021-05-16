using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Compilation;
using UnityEditor.Experimental;
using UnityEngine;

namespace NeroWeNeed.UIECS.Editor
{
    [ScriptedImporter(1, Extension)]
    public class UIModelImporter : ScriptedImporter
    {
        public const string Extension = "uim";
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var serializer = new XmlSerializer(typeof(UIModel));
            UIModel model = default;
            using (var fs = File.OpenRead(ctx.assetPath))
            {
                model = (UIModel)serializer.Deserialize(fs);
            }
            if (model == null)
            {
                ctx.LogImportError($"Unable to Import {ctx.assetPath}");
                return;
            }
            if (model.name == null)
            {
                var nameStart = ctx.assetPath.LastIndexOf('/') + 1;
                var nameEnd = ctx.assetPath.LastIndexOf('.');
                model.name = ctx.assetPath.Substring(nameStart, nameEnd - nameStart);
            }
            if (model.assembly == null)
            {
                model.assembly = $"{Application.productName}.UI.Models.{model.name}";
            }
            var asset = model.ToAsset();
            Debug.Log(asset.properties.Count);
            ctx.AddObjectToAsset("Model", asset);

            var directory = ctx.assetPath.Substring(0, ctx.assetPath.LastIndexOf('/'));
            if (UIModelAssemblyBuilder.BuildAssembly(directory, null, model, out string assemblyPath))
            {
                AssetDatabase.ImportAsset(assemblyPath);
                CompilationPipeline.RequestScriptCompilation();
            }
        }
    }

    [XmlRoot("Model")]
    public class UIModel
    {
        private static readonly IUIModelPropertyProvider defaultProvider = new UIModelValuePropertyProvider();
        private static readonly Dictionary<string, string> typeAlises = new Dictionary<string, string>() {
            { "sbyte", typeof(sbyte).FullName },
            { "short", typeof(short).FullName },
            { "int", typeof(int).FullName },
            { "long", typeof(long).FullName },
            { "byte", typeof(byte).FullName },
            { "ushort", typeof(ushort).FullName },
            { "uint", typeof(uint).FullName },
            { "ulong", typeof(ulong).FullName },
            { "float", typeof(float).FullName },
            { "double", typeof(double).FullName },
            { "string", typeof(string).FullName }
        };
        private static readonly Dictionary<string, IUIModelPropertyProvider> providers = new Dictionary<string, IUIModelPropertyProvider>() {
            { typeof(string).FullName, new UIModelStringPropertyProvider() }
        };
        [XmlArray("Properties")]
        public List<Property> properties;
        [XmlAttribute("assembly")]
        public string assembly;
        [XmlAttribute("name")]
        public string name;
        [XmlAttribute("namespace")]
        public string @namespace;
        public UIModelAsset ToAsset()
        {
            var asset = ScriptableObject.CreateInstance<UIModelAsset>();
            asset.modelName = name;
            asset.assembly = assembly;
            asset.properties = new List<UIModelAssetBaseProperty>();
            foreach (var property in properties)
            {
                asset.properties.Add(GetProvider(property).CreateProperty(this, property));
            }
            return asset;
        }
        public IUIModelPropertyProvider GetProvider(Property property)
        {

            if (providers.TryGetValue(property.ResolvedType, out var provider))
            {
                return provider;
            }
            else
            {
                return defaultProvider;
            }
        }
        public class Property
        {
            [XmlAttribute("name")]
            public string name;
            [XmlAttribute("type")]
            public string type;
            [XmlIgnore]
            private string resolvedType;
            [XmlAttribute("value")]
            public string value;
            public string ResolvedType
            {
                get
                {
                    if (resolvedType == null)
                    {
                        if (type.EndsWith("[]"))
                        {
                            if (typeAlises.TryGetValue(type.Substring(0, type.Length - 2), out var newTypeName))
                            {
                                resolvedType = $"{newTypeName}[]";
                            }
                            else
                            {
                                resolvedType = type;
                            }
                        }
                        else
                        {
                            if (typeAlises.TryGetValue(type, out var newTypeName))
                            {
                                resolvedType = newTypeName;
                            }
                            else
                            {
                                resolvedType = type;
                            }
                        }
                    }
                    return resolvedType;
                }
            }


        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using NeroWeNeed.Commons.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace NeroWeNeed.UIECS.Editor
{
    [ScriptedImporter(1, Extension)]
    public class UIViewImporter : ScriptedImporter
    {
        public const string Extension = "ui";
        private static readonly INodeAttributeImporter[] AttributeImporters = new INodeAttributeImporter[] {
            new NameAttributeImporter(),
            new ClassAttributeImporter(),
            new MaterialAttributeImporter(),
            new ModelPropertyAttributeImporter()
        };


        public override void OnImportAsset(AssetImportContext ctx)
        {

            var asset = ScriptableObject.CreateInstance<UIViewAsset>();
            var settings = UIGlobalSettings.instance;
            ImportXmlIntoAsset(asset, AssetDatabase.GUIDFromAssetPath(ctx.assetPath).ToString(), settings, ctx.assetPath);
            ctx.AddObjectToAsset("UI", asset);
            ctx.SetMainObject(asset);
        }
        private static void ImportXmlIntoAsset(UIViewAsset asset, string guid, UIGlobalSettings settings, string path)
        {
            using var fs = File.Open(path, FileMode.Open);
            var serializer = new XmlSerializer(typeof(UIViewXml));
            UIViewXml obj = (UIViewXml)serializer.Deserialize(fs);
            asset.group = settings.GetOrCreateGroup(obj.group);
            asset.referencedAssets = new List<UnityEngine.Object>();
            asset.nodes = new List<UIViewAsset.Node>();
            var entryReference = asset.group.GetEntryReference(guid);
            var groupEntry = new UIGroup.GroupEntry(guid);

            if (obj.root != null)
            {
                CreateNode(settings, obj.root, asset, groupEntry,-1);
            }
            asset.group.UpdateEntry(entryReference, groupEntry);
            asset.referencedAssets = asset.referencedAssets.Distinct().ToList();
        }
        private static int CreateNode(UIGlobalSettings settings, XmlNode xmlNode, UIViewAsset asset, UIGroup.GroupEntry groupEntry,int parentIndex)
        {
            var elementName = string.IsNullOrWhiteSpace(xmlNode.NamespaceURI) ? xmlNode.LocalName : xmlNode.NamespaceURI + '.' + xmlNode.LocalName;
            if (UIElementManager.TryGetElement(elementName, out ulong hash))
            {
                var node = new UIViewAsset.Node
                {
                    type = UIElementManager.GetElement(hash),
                    parentIndex = parentIndex,
                    index = asset.nodes.Count
                };
                var nodeProperties = new List<UIViewAsset.Node.Property>();
                for (int i = 0; i < xmlNode.Attributes.Count; i++)
                {
                    var attribute = xmlNode.Attributes[i];
                    var imported = false;
                    
                    foreach (var importer in AttributeImporters)
                    {
                        if (importer.ShouldImport(attribute))
                        {
                            importer.Import(attribute, node);
                            imported = true;
                            break;
                        }
                    }
                    if (!imported)
                    {
                        var field = UIElementManager.GetField(attribute.Name);
                        var groupAttribute = field.FieldType.GetCustomAttribute<GroupElementAttribute>();
                        if (groupAttribute != null)
                        {

                            switch (groupAttribute.type)
                            {
                                case GroupElementType.None:
                                    break;
                                case GroupElementType.Image:
                                    groupEntry.images.Add(UIEditorUtility.LoadUIAsset<Texture>(attribute.Value));
                                    break;
                            }
                            //asset.group.AddGroupData(asset, attribute.Value, groupAttribute.type);
                        }
                        nodeProperties.Add(new UIViewAsset.Node.Property { name = attribute.Name, value = attribute.Value });
                        var assetAttr = field.GetCustomAttribute<UIUnityObjectAssetAttribute>();
                        if (assetAttr != null)
                        {
                            var type = assetAttr.type;
                            var referencedAsset = AssetDatabase.LoadAssetAtPath(attribute.Value, assetAttr.type);
                            asset.referencedAssets.Add(referencedAsset);
                        }
                    }
                }
                node.properties = nodeProperties.ToArray();
                asset.nodes.Add(node);
                if (xmlNode.HasChildNodes)
                {
                    var children = new List<int>();
                    for (int i = 0; i < xmlNode.ChildNodes.Count; i++)
                    {
                        children.Add(CreateNode(settings, xmlNode.ChildNodes[i], asset, groupEntry,node.index));
                    }
                    node.childrenIndices = children.ToArray();
                }
                return node.index;
            }
            else
            {
                throw new Exception($"Unknown Element {elementName}");
            }
        }
        [XmlRoot("UI")]
        public class UIViewXml
        {

            [XmlAttribute("group")]
            public string group;
            [XmlAnyElement]
            public XmlNode root;

        }
    }
    internal interface INodeAttributeImporter
    {
        public bool ShouldImport(XmlAttribute attribute);
        public void Import(XmlAttribute attribute, UIViewAsset.Node node);
    }
    internal class NameAttributeImporter : INodeAttributeImporter
    {
        public const string AttributeName = "name";
        public void Import(XmlAttribute attribute, UIViewAsset.Node node)
        {
            node.name = attribute.Value;
        }

        public bool ShouldImport(XmlAttribute attribute)
        {
            return string.Equals(AttributeName, attribute.Name, StringComparison.InvariantCultureIgnoreCase);
        }
    }
    internal class ClassAttributeImporter : INodeAttributeImporter
    {
        public const string AttributeName = "class";
        public void Import(XmlAttribute attribute, UIViewAsset.Node node)
        {
            node.classes = attribute.Value.Split(' ');
        }

        public bool ShouldImport(XmlAttribute attribute)
        {
            return string.Equals(AttributeName, attribute.Name, StringComparison.InvariantCultureIgnoreCase);
        }
    }
    internal class MaterialAttributeImporter : INodeAttributeImporter
    {
        public const string AttributeName = "material";
        public void Import(XmlAttribute attribute, UIViewAsset.Node node)
        {
            node.material = UIEditorUtility.LoadUIAsset<Material>(attribute.Value);
        }

        public bool ShouldImport(XmlAttribute attribute)
        {
            return string.Equals(AttributeName, attribute.Name, StringComparison.InvariantCultureIgnoreCase);
        }
    }
    internal class ModelPropertyAttributeImporter : INodeAttributeImporter
    {
        public const string AttributeName = "model-property";
        public void Import(XmlAttribute attribute, UIViewAsset.Node node)
        {
            node.modelPropertyBinding = attribute.Value;
        }

        public bool ShouldImport(XmlAttribute attribute)
        {
            return string.Equals(AttributeName, attribute.Name, StringComparison.InvariantCultureIgnoreCase);
        }
    }

}

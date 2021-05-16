using System;
using System.IO;
using System.Linq;
using NeroWeNeed.Commons;
using NeroWeNeed.Commons.Editor;
using UnityEditor;
using UnityEngine;
namespace NeroWeNeed.UIECS.Editor
{
    [FilePath("ProjectSettings/UIGlobalSettings.asset",FilePathAttribute.Location.ProjectFolder)]
    public class UIGlobalSettings : ScriptableSingleton<UIGlobalSettings>
    {

        public const string DefaultGroupDirectory = "Assets/UI/Groups";
        public const string DefaultArtifactDirectory = "Assets/Resources/UI/Artifacts";
        public const string DefaultModelSchemaDirectory = "Assets/UI/Models";
        public const string DefaultArtifactAddressablesDirectory = "Assets/ResourceData/UI/Artifacts";
        public const string DefaultMaterialCacheDirectory = "Assets/Resources/UI/Materials";
        public const string DefaultMaterialCacheAddressablesDirectory = "Assets/ResourceData/UI/Materials";
        public const string DefaultGroupName = "default";
        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider()
        {

            return new ProjectGlobalSettingsProvider<UIGlobalSettings>("Project/UIGlobalSettings", SettingsScope.Project)
            {
                label = "UI Settings"
            };
        }
        public string groupDirectory = DefaultGroupDirectory;
        public string modelSchemaDirectory = DefaultModelSchemaDirectory;
        public string materialCacheDirectory = DefaultMaterialCacheAddressablesDirectory;
        public string artifactDirectory = DefaultArtifactAddressablesDirectory;
        public UIGroup GetDefaultGroup() => GetOrCreateGroup(DefaultGroupName);
        public UIGroup GetOrCreateGroup(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = DefaultGroupName;
            }
            var groupGuid = AssetDatabase.FindAssets($"t:{nameof(UIGroup)} l:UIGroup-{name}").FirstOrDefault();
            UIGroup groupObj;
            if (groupGuid == null)
            {
                groupObj = ScriptableObject.CreateInstance<UIGroup>();
                groupObj.groupName = name;
                if (!Directory.Exists(groupDirectory))
                {
                    Directory.CreateDirectory(groupDirectory);
                }
                AssetDatabase.CreateAsset(groupObj, $"{groupDirectory}/{Guid.NewGuid():N}.asset");
                AssetDatabase.SetLabels(groupObj, new string[] { $"UIGroup-{name}" });
            }
            else
            {
                groupObj = AssetDatabase.LoadAssetAtPath<UIGroup>(AssetDatabase.GUIDToAssetPath(groupGuid));
            }
            return groupObj;
        }
        public string CreateArtifactPath()
        {
            if (!Directory.Exists(artifactDirectory))
            {
                Directory.CreateDirectory(artifactDirectory);
            }
            return $"{artifactDirectory}/{Guid.NewGuid():N}.bytes";
        }
        private void OnValidate()
        {
            var needsRefresh = false;
            needsRefresh = GeneralUtility.FixDirectory(ref groupDirectory, DefaultGroupDirectory) || needsRefresh;
#if ADDRESSABLES_EXIST
            needsRefresh = GeneralUtility.FixDirectory(ref materialCacheDirectory, DefaultMaterialCacheAddressablesDirectory) || needsRefresh;
            needsRefresh = GeneralUtility.FixDirectory(ref artifactDirectory, DefaultArtifactAddressablesDirectory) || needsRefresh;
#else
            needsRefresh = GeneralUtility.FixDirectory(ref materialCacheDirectory, DefaultMaterialCacheDirectory) || needsRefresh;
            needsRefresh = GeneralUtility.FixDirectory(ref artifactDirectory, DefaultArtifactDirectory) || needsRefresh;
#endif
            if (needsRefresh)
            {
                EditorUtility.SetDirty(this);
            }
        }

    }
}
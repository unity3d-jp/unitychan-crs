/*
 * Copyright 2019,2020 Sony Corporation
 */

using UnityEditor;
#if UNITY_2019_1_OR_NEWER
    using UnityEngine.UIElements;
#else
    using UnityEngine.Experimental.UIElements;
#endif

using SRD.Core;
using SRD.Utils;

namespace SRD.Editor
{
    internal class SRDProjectSettingsAsset
    {
        private const string AssetPath = SRDHelper.SRDConstants.SRDProjectSettingsAssetPath;
        private static SRDProjectSettings GetOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SRDProjectSettings>(AssetPath);
            if(settings == null)
            {
                return Create();
            }
            else
            {
                return settings;
            }
        }

        private static SRDProjectSettings Create()
        {
            var directoryPath = System.IO.Path.GetDirectoryName(AssetPath);
            System.IO.Directory.CreateDirectory(directoryPath);

            var instance = SRDProjectSettings.GetDefault();
            AssetDatabase.CreateAsset(instance, AssetPath);
            AssetDatabase.SaveAssets();
            return instance;
        }

        internal static SerializedObject GetMutable()
        {
            return new SerializedObject(GetOrCreate());
        }

        public static SRDProjectSettings Get()
        {
            return GetOrCreate();
        }

        public static bool Exists()
        {
            return System.IO.File.Exists(AssetPath);
        }
    }

    internal class SRDProjectSettingsProvider : SettingsProvider
    {
        private SerializedObject mutableSettings;

        public SRDProjectSettingsProvider(string path, SettingsScope scope)
            : base(path, scope)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            mutableSettings = SRDProjectSettingsAsset.GetMutable();
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.PropertyField(mutableSettings.FindProperty("RunWithoutSRDisplay"));
            mutableSettings.ApplyModifiedProperties();
        }
    }

    static class SRDProjectSettingsRegister
    {
        [SettingsProvider]
        private static SettingsProvider CreateProviderToRegister()
        {
            var path = "Project/Spatial Reality Display";
            var provider = new SRDProjectSettingsProvider(path, SettingsScope.Project);
            return provider;
        }
    }
}

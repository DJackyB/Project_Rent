using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Martian.Localization.Editor
{
    public sealed class LocalizationProjectSetupConfig : ScriptableObject
    {
        internal const string DefaultAssetPath = "Assets/_Assets/Martian/Localization/Editor/LocalizationProjectSetupConfig.asset";

        [SerializeField] private string projectFolder = "Assets/_Assets/Martian/Localization/Project";
        [SerializeField] private string defaultLanguageCode = "zh-Hans";
        [SerializeField] private string fallbackLanguageCode = "en";
        [SerializeField] private List<string> stringTableNames = new() { "UI", "Common", "Card" };
        [SerializeField] private List<string> assetScanRoots = new() { "Assets" };
        [SerializeField] private bool replaceFontsInPrefabs = true;
        [SerializeField] private bool replaceFontsInScenes = true;
        [SerializeField] private bool initializeSynchronously = true;
        [SerializeField] private bool preloadTables = true;

        public string ProjectFolder => projectFolder;
        public string DefaultLanguageCode => defaultLanguageCode;
        public string FallbackLanguageCode => fallbackLanguageCode;
        public IReadOnlyList<string> StringTableNames => stringTableNames;
        public IReadOnlyList<string> AssetScanRoots => assetScanRoots;
        public bool ReplaceFontsInPrefabs => replaceFontsInPrefabs;
        public bool ReplaceFontsInScenes => replaceFontsInScenes;
        public bool InitializeSynchronously => initializeSynchronously;
        public bool PreloadTables => preloadTables;

        public void EnsureDefaults()
        {
            if (string.IsNullOrWhiteSpace(projectFolder))
            {
                projectFolder = "Assets/_Assets/Martian/Localization/Project";
            }

            if (string.IsNullOrWhiteSpace(defaultLanguageCode))
            {
                defaultLanguageCode = "zh-Hans";
            }

            if (string.IsNullOrWhiteSpace(fallbackLanguageCode))
            {
                fallbackLanguageCode = "en";
            }

            if (stringTableNames == null || stringTableNames.Count == 0)
            {
                stringTableNames = new List<string> { "UI", "Common", "Card" };
            }

            if (assetScanRoots == null || assetScanRoots.Count == 0)
            {
                assetScanRoots = new List<string> { "Assets" };
            }
        }

        public static LocalizationProjectSetupConfig CreateTransientDefault()
        {
            var config = CreateInstance<LocalizationProjectSetupConfig>();
            config.EnsureDefaults();
            config.hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideAndDontSave;
            return config;
        }

        public static LocalizationProjectSetupConfig LoadFirstOrCreateTransient()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(LocalizationProjectSetupConfig)}");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var asset = AssetDatabase.LoadAssetAtPath<LocalizationProjectSetupConfig>(path);
                if (asset != null)
                {
                    asset.EnsureDefaults();
                    return asset;
                }
            }

            return CreateTransientDefault();
        }
    }
}

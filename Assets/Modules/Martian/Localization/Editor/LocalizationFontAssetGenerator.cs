using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Martian.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.TextCore.LowLevel;

namespace Martian.Localization.Editor
{
    public static class LocalizationFontAssetGenerator
    {
        private const string SetupMenuRoot = "Tools/Martian/Localization/Advanced/Setup Project Localization";
        private const string FontsFolder = "Assets/Modules/Martian/Localization/Resources/Fonts/SourceHanSansSC";
        private const string RegularFontPath = FontsFolder + "/SourceHanSansSC-Regular.otf";
        private const string BoldFontPath = FontsFolder + "/SourceHanSansSC-Bold.otf";
        private const string RegularFontAssetPath = FontsFolder + "/SourceHanSansSC-Regular SDF.asset";
        private const string BoldFontAssetPath = FontsFolder + "/SourceHanSansSC-Bold SDF.asset";
        private const string LocalizationFontProfilePath = "Assets/Modules/Martian/Localization/Resources/Localization/LocalizationFontProfile.asset";
        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasSize = 1024;

        [MenuItem(SetupMenuRoot)]
        public static void SetupBundledChineseFonts()
        {
            SetupProjectLocalization(LocalizationProjectSetupConfig.LoadFirstOrCreateTransient());
        }

        [MenuItem(SetupMenuRoot, true)]
        public static bool ValidateSetupBundledChineseFonts()
        {
            return HasBundledSourceFonts();
        }

        public static void SetupProjectLocalization(LocalizationProjectSetupConfig config)
        {
            if (!HasBundledSourceFonts())
            {
                Debug.LogError("[Martian.Localization] Bundled source fonts are missing. Reimport the plugin fonts first.");
                return;
            }

            if (TMP_Settings.instance == null)
            {
                Debug.LogError("[Martian.Localization] TMP Settings not found. Import TMP Essential Resources first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Martian.Localization] Setup cancelled because open scene changes were not saved.");
                return;
            }

            SetupOptions options = SetupOptions.FromConfig(config);

            GenerateBundledFonts();

            TMP_FontAsset baseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularFontAssetPath);
            TMP_FontAsset boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontAssetPath);
            if (baseFont == null)
            {
                Debug.LogError("[Martian.Localization] Base TMP font asset was not generated successfully.");
                return;
            }

            LocalizationFontProfile profile = EnsureLocalizationFontProfile();
            BridgeLocalizationProfile(profile, options.DefaultLanguageCode, baseFont, boldFont);
            SetupLocalizationProjectAssets(options);

            int updatedPrefabCount = options.ReplaceFontsInPrefabs ? ReplaceFontsInAllPrefabs(baseFont, options.AssetScanRoots) : 0;
            int updatedSceneCount = options.ReplaceFontsInScenes ? ReplaceFontsInAllScenes(baseFont, options.AssetScanRoots) : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Martian.Localization] Project localization setup complete. " +
                $"Default locale '{options.DefaultLanguageCode}', fallback '{options.FallbackLanguageCode}', " +
                $"tables [{string.Join(", ", options.StringTableNames)}], " +
                $"updated {updatedPrefabCount} prefab(s) and {updatedSceneCount} scene(s).");
        }

        private static bool HasBundledSourceFonts()
        {
            return AssetDatabase.LoadAssetAtPath<Font>(RegularFontPath) != null
                && AssetDatabase.LoadAssetAtPath<Font>(BoldFontPath) != null;
        }

        private static void GenerateBundledFonts()
        {
            GenerateFontAsset(RegularFontPath, "SourceHanSansSC-Regular SDF.asset");
            GenerateFontAsset(BoldFontPath, "SourceHanSansSC-Bold SDF.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GenerateFontAsset(string sourceFontPath, string outputFileName)
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);
            if (sourceFont == null)
            {
                throw new FileNotFoundException($"Source font not found at '{sourceFontPath}'.");
            }

            string outputPath = $"{FontsFolder}/{outputFileName}";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
            {
                AssetDatabase.DeleteAsset(outputPath);
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null)
            {
                throw new InvalidOperationException($"TMP font asset creation returned null for '{sourceFontPath}'.");
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(outputFileName);
            AssetDatabase.CreateAsset(fontAsset, outputPath);

            if (fontAsset.material != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    Texture2D atlasTexture = fontAsset.atlasTextures[i];
                    if (atlasTexture != null)
                    {
                        AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
                    }
                }
            }

            EditorUtility.SetDirty(fontAsset);
        }

        private static LocalizationFontProfile EnsureLocalizationFontProfile()
        {
            LocalizationFontProfile profile = AssetDatabase.LoadAssetAtPath<LocalizationFontProfile>(LocalizationFontProfilePath);
            if (profile != null)
            {
                return profile;
            }

            EnsureFolder(Path.GetDirectoryName(LocalizationFontProfilePath)?.Replace("\\", "/"));

            profile = ScriptableObject.CreateInstance<LocalizationFontProfile>();
            AssetDatabase.CreateAsset(profile, LocalizationFontProfilePath);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void BridgeLocalizationProfile(
            LocalizationFontProfile profile,
            string languageCode,
            TMP_FontAsset baseFont,
            TMP_FontAsset boldFont)
        {
            SerializedObject serializedProfile = new SerializedObject(profile);
            SerializedProperty mappings = serializedProfile.FindProperty("mappings");

            int targetIndex = FindMappingIndex(mappings, languageCode);
            if (targetIndex < 0)
            {
                targetIndex = mappings.arraySize;
                mappings.InsertArrayElementAtIndex(targetIndex);
            }

            SerializedProperty mapping = mappings.GetArrayElementAtIndex(targetIndex);
            mapping.FindPropertyRelative("languageCode").stringValue = languageCode;
            mapping.FindPropertyRelative("fontAsset").objectReferenceValue = baseFont;

            SerializedProperty fallbackFonts = mapping.FindPropertyRelative("fallbackFontAssets");
            fallbackFonts.ClearArray();
            if (boldFont != null)
            {
                fallbackFonts.InsertArrayElementAtIndex(0);
                fallbackFonts.GetArrayElementAtIndex(0).objectReferenceValue = boldFont;
            }

            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        private static void SetupLocalizationProjectAssets(SetupOptions options)
        {
            EnsureFolder(options.LocalizationProjectFolder);
            EnsureFolder(options.LocalizationSettingsFolder);
            EnsureFolder(options.LocalizationLocalesFolder);
            EnsureFolder(options.LocalizationTablesFolder);

            LocalizationSettings settings = EnsureLocalizationSettings(options);
            Locale defaultLocale = EnsureLocale(options.DefaultLanguageCode, options.LocalizationLocalesFolder);
            Locale fallbackLocale = EnsureLocale(options.FallbackLanguageCode, options.LocalizationLocalesFolder);

            if (defaultLocale == null || fallbackLocale == null)
            {
                throw new InvalidOperationException("[Martian.Localization] Failed to create required locale assets.");
            }

            EnsureStringTableCollections(options, defaultLocale, fallbackLocale);
            ConfigureLocalizationSettings(settings, defaultLocale, options.InitializeSynchronously);
        }

        private static LocalizationSettings EnsureLocalizationSettings(SetupOptions options)
        {
            LocalizationSettings settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(options.LocalizationSettingsAssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "Localization Settings";
                AssetDatabase.CreateAsset(settings, options.LocalizationSettingsAssetPath);
            }

            if (LocalizationEditorSettings.ActiveLocalizationSettings != settings)
            {
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static Locale EnsureLocale(string languageCode, string localesFolder)
        {
            Locale locale = LocalizationEditorSettings.GetLocale(languageCode);
            if (locale != null)
            {
                return locale;
            }

            string localeAssetPath = $"{localesFolder}/{languageCode}.asset";
            locale = AssetDatabase.LoadAssetAtPath<Locale>(localeAssetPath);
            if (locale == null)
            {
                locale = Locale.CreateLocale(languageCode);
                locale.name = languageCode;
                AssetDatabase.CreateAsset(locale, localeAssetPath);
            }

            if (LocalizationEditorSettings.GetLocale(languageCode) == null)
            {
                LocalizationEditorSettings.AddLocale(locale);
            }

            EditorUtility.SetDirty(locale);
            return locale;
        }

        private static void EnsureStringTableCollections(SetupOptions options, params Locale[] locales)
        {
            Locale[] validLocales = locales
                .Where(locale => locale != null)
                .GroupBy(locale => locale.Identifier.Code, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();
            if (validLocales.Length == 0)
            {
                throw new InvalidOperationException("[Martian.Localization] No locales are available for table creation.");
            }

            for (int i = 0; i < options.StringTableNames.Length; i++)
            {
                string tableName = options.StringTableNames[i];
                StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null)
                {
                    collection = LocalizationEditorSettings.CreateStringTableCollection(tableName, options.LocalizationTablesFolder, validLocales);
                }

                bool collectionChanged = false;
                for (int localeIndex = 0; localeIndex < validLocales.Length; localeIndex++)
                {
                    Locale locale = validLocales[localeIndex];
                    if (collection.GetTable(locale.Identifier) != null)
                    {
                        continue;
                    }

                    collection.AddNewTable(locale.Identifier);
                    collectionChanged = true;
                }

                foreach (StringTable table in collection.StringTables)
                {
                    LocalizationEditorSettings.SetPreloadTableFlag(table, options.PreloadTables);
                    EditorUtility.SetDirty(table);
                }

                if (collection.SharedData != null)
                {
                    EditorUtility.SetDirty(collection.SharedData);
                }

                if (collectionChanged)
                {
                    EditorUtility.SetDirty(collection);
                }
            }
        }

        private static void ConfigureLocalizationSettings(LocalizationSettings settings, Locale defaultLocale, bool initializeSynchronously)
        {
            SerializedObject serializedSettings = new SerializedObject(settings);
            SerializedProperty startupSelectors = serializedSettings.FindProperty("m_StartupSelectors");
            if (startupSelectors != null)
            {
                for (int i = 0; i < startupSelectors.arraySize; i++)
                {
                    SerializedProperty selector = startupSelectors.GetArrayElementAtIndex(i);
                    SerializedProperty localeCode = selector.FindPropertyRelative("m_LocaleId.m_Code");
                    if (localeCode != null)
                    {
                        localeCode.stringValue = defaultLocale.Identifier.Code;
                    }
                }

                serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            }

            LocalizationSettings.Instance = settings;
            LocalizationSettings.ProjectLocale = defaultLocale;
            LocalizationSettings.SelectedLocale = defaultLocale;
            LocalizationSettings.InitializeSynchronously = initializeSynchronously;
            EditorUtility.SetDirty(settings);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string parentFolder = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            string folderName = Path.GetFileName(assetPath);
            if (string.IsNullOrWhiteSpace(parentFolder) || string.IsNullOrWhiteSpace(folderName))
            {
                throw new InvalidOperationException($"[Martian.Localization] Invalid folder path '{assetPath}'.");
            }

            EnsureFolder(parentFolder);
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }

        private static int FindMappingIndex(SerializedProperty mappings, string languageCode)
        {
            for (int i = 0; i < mappings.arraySize; i++)
            {
                SerializedProperty mapping = mappings.GetArrayElementAtIndex(i);
                if (mapping.FindPropertyRelative("languageCode").stringValue == languageCode)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int ReplaceFontsInAllPrefabs(TMP_FontAsset baseFont, IReadOnlyList<string> scanRoots)
        {
            int updatedPrefabCount = 0;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", scanRoots.ToArray());

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefabRoot = null;

                try
                {
                    prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (!ReplaceFontsInHierarchy(prefabRoot, baseFont))
                    {
                        continue;
                    }

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    updatedPrefabCount++;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[Martian.Localization] Skipped prefab '{prefabPath}' while replacing TMP fonts. {exception.Message}");
                }
                finally
                {
                    if (prefabRoot != null)
                    {
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                }
            }

            return updatedPrefabCount;
        }

        private static int ReplaceFontsInAllScenes(TMP_FontAsset baseFont, IReadOnlyList<string> scanRoots)
        {
            int updatedSceneCount = 0;
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", scanRoots.ToArray());

            try
            {
                for (int i = 0; i < sceneGuids.Length; i++)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                    try
                    {
                        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                        bool changed = false;
                        GameObject[] roots = scene.GetRootGameObjects();
                        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                        {
                            changed |= ReplaceFontsInHierarchy(roots[rootIndex], baseFont);
                        }

                        if (!changed)
                        {
                            continue;
                        }

                        EditorSceneManager.SaveScene(scene);
                        updatedSceneCount++;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"[Martian.Localization] Skipped scene '{scenePath}' while replacing TMP fonts. {exception.Message}");
                    }
                }
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            return updatedSceneCount;
        }

        private static bool ReplaceFontsInHierarchy(GameObject root, TMP_FontAsset baseFont)
        {
            bool changed = false;
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                if (text.font == baseFont && text.fontSharedMaterial == baseFont.material)
                {
                    continue;
                }

                Undo.RecordObject(text, "Replace TMP Font");
                text.font = baseFont;
                if (baseFont.material != null)
                {
                    text.fontSharedMaterial = baseFont.material;
                }

                text.UpdateMeshPadding();
                EditorUtility.SetDirty(text);
                changed = true;
            }

            return changed;
        }

        private sealed class SetupOptions
        {
            public string DefaultLanguageCode;
            public string FallbackLanguageCode;
            public string[] StringTableNames;
            public string[] AssetScanRoots;
            public string LocalizationProjectFolder;
            public string LocalizationSettingsFolder;
            public string LocalizationLocalesFolder;
            public string LocalizationTablesFolder;
            public string LocalizationSettingsAssetPath;
            public bool ReplaceFontsInPrefabs;
            public bool ReplaceFontsInScenes;
            public bool InitializeSynchronously;
            public bool PreloadTables;

            public static SetupOptions FromConfig(LocalizationProjectSetupConfig config)
            {
                LocalizationProjectSetupConfig safeConfig = config != null ? config : LocalizationProjectSetupConfig.CreateTransientDefault();
                safeConfig.EnsureDefaults();

                string projectFolder = NormalizeFolder(safeConfig.ProjectFolder, "Assets/Modules/Martian/Localization/Project");

                return new SetupOptions
                {
                    DefaultLanguageCode = SanitizeLanguageCode(safeConfig.DefaultLanguageCode, "zh-Hans"),
                    FallbackLanguageCode = SanitizeLanguageCode(safeConfig.FallbackLanguageCode, "en"),
                    StringTableNames = SanitizeTableNames(safeConfig.StringTableNames),
                    AssetScanRoots = SanitizeScanRoots(safeConfig.AssetScanRoots),
                    LocalizationProjectFolder = projectFolder,
                    LocalizationSettingsFolder = projectFolder + "/Settings",
                    LocalizationLocalesFolder = projectFolder + "/Locales",
                    LocalizationTablesFolder = projectFolder + "/Tables",
                    LocalizationSettingsAssetPath = projectFolder + "/Settings/Localization Settings.asset",
                    ReplaceFontsInPrefabs = safeConfig.ReplaceFontsInPrefabs,
                    ReplaceFontsInScenes = safeConfig.ReplaceFontsInScenes,
                    InitializeSynchronously = safeConfig.InitializeSynchronously,
                    PreloadTables = safeConfig.PreloadTables
                };
            }

            private static string NormalizeFolder(string value, string fallback)
            {
                string normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Replace("\\", "/").TrimEnd('/');
                return normalized.StartsWith("Assets", StringComparison.OrdinalIgnoreCase) ? normalized : fallback;
            }

            private static string SanitizeLanguageCode(string value, string fallback)
            {
                return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            }

            private static string[] SanitizeTableNames(IReadOnlyList<string> values)
            {
                if (values == null || values.Count == 0)
                {
                    return new[] { "UI", "Common", "Card" };
                }

                return values
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            private static string[] SanitizeScanRoots(IReadOnlyList<string> values)
            {
                if (values == null || values.Count == 0)
                {
                    return new[] { "Assets" };
                }

                string[] sanitized = values
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Replace("\\", "/").TrimEnd('/'))
                    .Where(value => value.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return sanitized.Length > 0 ? sanitized : new[] { "Assets" };
            }
        }
    }
}

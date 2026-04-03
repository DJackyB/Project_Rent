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
        private const string SetupMenuRoot = "Tools/Martian/Localization/Setup Project Localization";
        private const string FontsFolder = "Assets/_Assets/Martian/Localization/Resources/Fonts/SourceHanSansSC";
        private const string RegularFontPath = FontsFolder + "/SourceHanSansSC-Regular.otf";
        private const string BoldFontPath = FontsFolder + "/SourceHanSansSC-Bold.otf";
        private const string RegularFontAssetPath = FontsFolder + "/SourceHanSansSC-Regular SDF.asset";
        private const string BoldFontAssetPath = FontsFolder + "/SourceHanSansSC-Bold SDF.asset";
        private const string LocalizationFontProfilePath = "Assets/_Assets/Martian/Localization/Resources/Localization/LocalizationFontProfile.asset";
        private const string LocalizationProjectFolder = "Assets/_Assets/Martian/Localization/Project";
        private const string LocalizationSettingsFolder = LocalizationProjectFolder + "/Settings";
        private const string LocalizationLocalesFolder = LocalizationProjectFolder + "/Locales";
        private const string LocalizationTablesFolder = LocalizationProjectFolder + "/Tables";
        private const string LocalizationSettingsAssetPath = LocalizationSettingsFolder + "/Localization Settings.asset";
        private const string DefaultLanguageCode = "zh-Hans";
        private const string DefaultFallbackLanguageCode = "en";
        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasSize = 1024;
        private static readonly string[] DefaultStringTableNames = { "UI", "Common", "Card" };

        private static void GenerateBundledFonts()
        {
            if (TMP_Settings.instance == null)
            {
                Debug.LogError("[Martian.Localization] TMP Settings not found. Import TMP Essential Resources first.");
                return;
            }

            try
            {
                GenerateFontAsset(RegularFontPath, "SourceHanSansSC-Regular SDF.asset");
                GenerateFontAsset(BoldFontPath, "SourceHanSansSC-Bold SDF.asset");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[Martian.Localization] Bundled TMP font assets generated successfully.");
            }
            catch (System.SystemException exception)
            {
                Debug.LogError($"[Martian.Localization] Failed to generate TMP font assets. {exception.Message}");
                throw;
            }
        }

        [MenuItem(SetupMenuRoot)]
        public static void SetupBundledChineseFonts()
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

            GenerateBundledFonts();

            TMP_FontAsset baseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularFontAssetPath);
            TMP_FontAsset boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontAssetPath);

            if (baseFont == null)
            {
                Debug.LogError("[Martian.Localization] Base TMP font asset was not generated successfully.");
                return;
            }

            LocalizationFontProfile profile = EnsureLocalizationFontProfile();
            BridgeLocalizationProfile(profile, baseFont, boldFont);
            SetupLocalizationProjectAssets();

            int updatedPrefabCount = ReplaceFontsInAllPrefabs(baseFont);
            int updatedSceneCount = ReplaceFontsInAllScenes(baseFont);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Martian.Localization] Project localization setup complete. Bridged '{DefaultLanguageCode}' to '{baseFont.name}', " +
                $"ensured settings/locales/tables, updated {updatedPrefabCount} prefab(s) and {updatedSceneCount} scene(s).");
        }

        [MenuItem(SetupMenuRoot, true)]
        public static bool ValidateSetupBundledChineseFonts()
        {
            return HasBundledSourceFonts();
        }

        private static bool HasBundledSourceFonts()
        {
            return AssetDatabase.LoadAssetAtPath<Font>(RegularFontPath) != null
                && AssetDatabase.LoadAssetAtPath<Font>(BoldFontPath) != null;
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
                throw new System.InvalidOperationException($"TMP font asset creation returned null for '{sourceFontPath}'.");
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

            profile = ScriptableObject.CreateInstance<LocalizationFontProfile>();
            AssetDatabase.CreateAsset(profile, LocalizationFontProfilePath);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void BridgeLocalizationProfile(LocalizationFontProfile profile, TMP_FontAsset baseFont, TMP_FontAsset boldFont)
        {
            SerializedObject serializedProfile = new SerializedObject(profile);
            SerializedProperty mappings = serializedProfile.FindProperty("mappings");

            int targetIndex = FindMappingIndex(mappings, DefaultLanguageCode);
            if (targetIndex < 0)
            {
                targetIndex = mappings.arraySize;
                mappings.InsertArrayElementAtIndex(targetIndex);
            }

            SerializedProperty mapping = mappings.GetArrayElementAtIndex(targetIndex);
            mapping.FindPropertyRelative("languageCode").stringValue = DefaultLanguageCode;
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

        private static void SetupLocalizationProjectAssets()
        {
            EnsureFolder(LocalizationProjectFolder);
            EnsureFolder(LocalizationSettingsFolder);
            EnsureFolder(LocalizationLocalesFolder);
            EnsureFolder(LocalizationTablesFolder);

            LocalizationSettings settings = EnsureLocalizationSettings();
            Locale chineseLocale = EnsureLocale(DefaultLanguageCode);
            Locale englishLocale = EnsureLocale(DefaultFallbackLanguageCode);

            if (chineseLocale == null || englishLocale == null)
            {
                throw new System.InvalidOperationException("[Martian.Localization] Failed to create required locale assets.");
            }

            EnsureStringTableCollections(chineseLocale, englishLocale);
            ConfigureLocalizationSettings(settings, chineseLocale);
        }

        private static LocalizationSettings EnsureLocalizationSettings()
        {
            LocalizationSettings settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(LocalizationSettingsAssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "Default Localization Settings";
                AssetDatabase.CreateAsset(settings, LocalizationSettingsAssetPath);
            }

            if (LocalizationEditorSettings.ActiveLocalizationSettings != settings)
            {
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static Locale EnsureLocale(string languageCode)
        {
            Locale locale = LocalizationEditorSettings.GetLocale(languageCode);
            if (locale != null)
            {
                return locale;
            }

            string localeAssetPath = $"{LocalizationLocalesFolder}/{languageCode}.asset";
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

        private static void EnsureStringTableCollections(params Locale[] locales)
        {
            Locale[] validLocales = locales.Where(locale => locale != null).ToArray();
            if (validLocales.Length == 0)
            {
                throw new System.InvalidOperationException("[Martian.Localization] No locales are available for table creation.");
            }

            for (int i = 0; i < DefaultStringTableNames.Length; i++)
            {
                string tableName = DefaultStringTableNames[i];
                StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null)
                {
                    collection = LocalizationEditorSettings.CreateStringTableCollection(tableName, LocalizationTablesFolder, validLocales);
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
                    LocalizationEditorSettings.SetPreloadTableFlag(table, true);
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

        private static void ConfigureLocalizationSettings(LocalizationSettings settings, Locale defaultLocale)
        {
            if (defaultLocale == null)
            {
                return;
            }

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
            LocalizationSettings.InitializeSynchronously = true;
            EditorUtility.SetDirty(settings);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string parentFolder = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            string folderName = Path.GetFileName(assetPath);
            if (string.IsNullOrWhiteSpace(parentFolder) || string.IsNullOrWhiteSpace(folderName))
            {
                throw new System.InvalidOperationException($"[Martian.Localization] Invalid folder path '{assetPath}'.");
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

        private static int ReplaceFontsInAllPrefabs(TMP_FontAsset baseFont)
        {
            int updatedPrefabCount = 0;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

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
                catch (System.Exception exception)
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

        private static int ReplaceFontsInAllScenes(TMP_FontAsset baseFont)
        {
            int updatedSceneCount = 0;
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });

            try
            {
                for (int i = 0; i < sceneGuids.Length; i++)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                    try
                    {
                        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

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
                    catch (System.Exception exception)
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
    }
}

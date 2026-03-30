using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Martian.Localization
{
    public static class LocalizationFontUtility
    {
        private const string FontProfileResourcePath = "Localization/LocalizationFontProfile";

        private static readonly Dictionary<string, TMP_FontAsset> CachedFontsByLanguage =
            new(StringComparer.OrdinalIgnoreCase);

        private static bool _sceneHookRegistered;

        public static void InvalidateCache()
        {
            CachedFontsByLanguage.Clear();
        }

        public static TMP_FontAsset GetPreferredFontAsset()
        {
            string languageCode = LocalizationServices.Language != null
                ? LocalizationServices.Language.CurrentLanguageCode
                : null;

            if (string.IsNullOrWhiteSpace(languageCode))
            {
                throw new InvalidOperationException(
                    "[LocalizationFontUtility] Language service is not initialized. " +
                    "Initialize localization before applying language-specific fonts.");
            }

            if (CachedFontsByLanguage.TryGetValue(languageCode, out TMP_FontAsset cached) && IsFontAssetUsable(cached))
            {
                return cached;
            }

            TMP_FontAsset resolved = ResolveConfiguredFont(languageCode);
            CachedFontsByLanguage[languageCode] = resolved;
            return resolved;
        }

        public static void ApplyToText(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            TMP_FontAsset fontAsset = GetPreferredFontAsset();
            if (text.font != fontAsset)
            {
                text.font = fontAsset;
            }

            if (fontAsset.material != null && text.fontSharedMaterial != fontAsset.material)
            {
                text.fontSharedMaterial = fontAsset.material;
            }

            text.UpdateMeshPadding();
        }

        public static void ApplyToChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                ApplyToText(text);
            }
        }

        public static void ApplyToAllLoadedSceneTexts()
        {
            RegisterSceneHooks();

            foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (text == null || text.gameObject == null)
                {
                    continue;
                }

                Scene scene = text.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                ApplyToText(text);
            }
        }

        private static TMP_FontAsset ResolveConfiguredFont(string languageCode)
        {
            LocalizationFontProfile profile = ResolveRequiredProfile();
            LocalizationFontProfile.LanguageFontMapping mapping = profile.GetRequiredMapping(languageCode);

            if (!IsFontAssetUsable(mapping.fontAsset))
            {
                throw new InvalidOperationException(
                    $"[LocalizationFontUtility] Invalid font asset mapping for language '{languageCode}'. " +
                    "Assign a valid TMP font asset in LocalizationFontProfile.");
            }

            ApplyFallbackFonts(mapping.fontAsset, mapping.fallbackFontAssets);
            return mapping.fontAsset;
        }

        private static LocalizationFontProfile ResolveRequiredProfile()
        {
            LocalizationFontProfile profile = LocalizationServices.FontProfile;
            if (profile != null)
            {
                return profile;
            }

            profile = Resources.Load<LocalizationFontProfile>(FontProfileResourcePath);
            if (profile == null)
            {
                throw new InvalidOperationException(
                    "[LocalizationFontUtility] Missing LocalizationFontProfile asset at " +
                    "'Resources/Localization/LocalizationFontProfile'. " +
                    "Configure fonts explicitly for every supported language.");
            }

            LocalizationServices.SetFontProfile(profile);
            return profile;
        }

        private static void ApplyFallbackFonts(TMP_FontAsset fontAsset, IList<TMP_FontAsset> fallbackFontAssets)
        {
            if (fontAsset == null)
            {
                return;
            }

            fontAsset.fallbackFontAssetTable.Clear();
            if (fallbackFontAssets == null)
            {
                return;
            }

            for (int i = 0; i < fallbackFontAssets.Count; i++)
            {
                TMP_FontAsset fallback = fallbackFontAssets[i];
                if (fallback != null && !fontAsset.fallbackFontAssetTable.Contains(fallback))
                {
                    fontAsset.fallbackFontAssetTable.Add(fallback);
                }
            }
        }

        private static bool IsFontAssetUsable(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return false;
            }

            try
            {
                return fontAsset.material != null
                    && fontAsset.atlasTextures != null
                    && fontAsset.atlasTextures.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHooks()
        {
            if (_sceneHookRegistered)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _sceneHookRegistered = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyCurrentSceneFonts()
        {
            ApplyToAllLoadedSceneTexts();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToAllLoadedSceneTexts();
        }
    }
}

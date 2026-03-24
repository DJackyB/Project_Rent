using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace BaoZuPo.UI
{
    public static class UIFontCatalog
    {
        public const string SourceFontAssetPath = "Assets/Resources/Fonts/SourceHanSansSC-Regular.otf";
        public const string GeneratedFontAssetPath = "Assets/Resources/Fonts/BaoZuPoChineseDynamic.asset";
        public const string SourceFontResourcePath = "Fonts/SourceHanSansSC-Regular";
        public const string GeneratedFontResourcePath = "Fonts/BaoZuPoChineseDynamic";

        private static TMP_FontAsset _cachedFontAsset;
        private static bool _loggedBrokenGeneratedFont;
        private static bool _loggedRuntimeFontCreationFailure;

        public static TMP_FontAsset GetPreferredFontAsset()
        {
            if (IsFontAssetUsable(_cachedFontAsset))
            {
                return _cachedFontAsset;
            }

            _cachedFontAsset = Resources.Load<TMP_FontAsset>(GeneratedFontResourcePath);
            if (IsFontAssetUsable(_cachedFontAsset))
            {
                return _cachedFontAsset;
            }

            if (_cachedFontAsset != null && !_loggedBrokenGeneratedFont)
            {
                _loggedBrokenGeneratedFont = true;
                Debug.LogWarning(
                    $"[UIFontCatalog] Generated TMP font '{GeneratedFontResourcePath}' is invalid " +
                    "(missing material or atlas). Falling back to a runtime-created font asset.");
            }

            var sourceFont = Resources.Load<Font>(SourceFontResourcePath);
            if (sourceFont != null)
            {
                TMP_FontAsset runtimeFont = TMP_FontAsset.CreateFontAsset(
                    sourceFont,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic);

                if (IsFontAssetUsable(runtimeFont))
                {
                    runtimeFont.name = "BaoZuPoChineseRuntime";
                    _cachedFontAsset = runtimeFont;
                    return _cachedFontAsset;
                }

                if (!_loggedRuntimeFontCreationFailure)
                {
                    _loggedRuntimeFontCreationFailure = true;
                    Debug.LogWarning(
                        $"[UIFontCatalog] Failed to create a valid runtime TMP font from '{SourceFontResourcePath}'. " +
                        "Falling back to default TMP font.");
                }
            }

            _cachedFontAsset = TMP_Settings.defaultFontAsset;
            if (IsFontAssetUsable(_cachedFontAsset))
            {
                return _cachedFontAsset;
            }

            _cachedFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return _cachedFontAsset;
        }

        public static void ApplyToText(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            TMP_FontAsset preferredFont = GetPreferredFontAsset();
            if (!IsFontAssetUsable(preferredFont))
            {
                return;
            }

            if (text.font != preferredFont)
            {
                text.font = preferredFont;
            }

            Material preferredMaterial = preferredFont.material;
            if (preferredMaterial != null && text.fontSharedMaterial != preferredMaterial)
            {
                text.fontSharedMaterial = preferredMaterial;
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

        private static bool IsFontAssetUsable(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return false;
            }

            Material material;
            Texture[] atlasTextures;

            try
            {
                material = fontAsset.material;
                atlasTextures = fontAsset.atlasTextures;
            }
            catch
            {
                return false;
            }

            if (material == null || atlasTextures == null || atlasTextures.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < atlasTextures.Length; i++)
            {
                if (atlasTextures[i] != null)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class UIFontBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyCurrentSceneFonts()
        {
            UIFontCatalog.ApplyToAllLoadedSceneTexts();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UIFontCatalog.ApplyToAllLoadedSceneTexts();
        }
    }
}

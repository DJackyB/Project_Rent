using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BaoZuPo.UI
{
    public static class UIFontCatalog
    {
        public const string SourceFontAssetPath = "Assets/Resources/Fonts/SourceHanSansSC-Regular.otf";
        public const string GeneratedFontAssetPath = "Assets/Resources/Fonts/BaoZuPoChineseDynamic.asset";
        public const string SourceFontResourcePath = "Fonts/SourceHanSansSC-Regular";
        public const string GeneratedFontResourcePath = "Fonts/BaoZuPoChineseDynamic";

        private static TMP_FontAsset _cachedFontAsset;

        public static TMP_FontAsset GetPreferredFontAsset()
        {
            if (_cachedFontAsset != null)
            {
                return _cachedFontAsset;
            }

            _cachedFontAsset = Resources.Load<TMP_FontAsset>(GeneratedFontResourcePath);
            if (_cachedFontAsset != null)
            {
                return _cachedFontAsset;
            }

            var sourceFont = Resources.Load<Font>(SourceFontResourcePath);
            if (sourceFont != null)
            {
                _cachedFontAsset = TMP_FontAsset.CreateFontAsset(
                    sourceFont,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic);

                if (_cachedFontAsset != null)
                {
                    _cachedFontAsset.name = "BaoZuPoChineseRuntime";
                    return _cachedFontAsset;
                }
            }

            _cachedFontAsset = TMP_Settings.defaultFontAsset;
            if (_cachedFontAsset != null)
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
            if (preferredFont == null || text.font == preferredFont)
            {
                return;
            }

            text.font = preferredFont;
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

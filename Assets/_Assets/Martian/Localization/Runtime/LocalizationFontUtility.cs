using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Martian.Localization
{
    /// <summary>
    /// 本地化字体工具。负责根据当前语言为 TextMeshPro 应用合适的字体。
    ///
    /// 职责：
    /// - 根据语言代码从 LocalizationFontProfile 查找对应的字体
    /// - 缓存字体以减少查询开销
    /// - 为 Text 组件应用字体及其备选字体
    /// - 监听场景加载，自动为新加载的 Text 应用字体
    ///
    /// 字体加载：
    /// 1. GetPreferredFontAsset() 根据当前语言查找字体
    /// 2. 若缓存中有，返回缓存值
    /// 3. 否则从 LocalizationFontProfile 查询，写入缓存
    /// 4. 字体不可用时抛出异常（配置错误）
    ///
    /// 场景字体应用：
    /// - 在 BeforeSceneLoad 注册场景加载钩子
    /// - 在 AfterSceneLoad 应用所有已加载场景的 Text
    /// - 每次场景加载时自动应用字体
    /// </summary>
    public static class LocalizationFontUtility
    {
        /// <summary>字体配置文件的资源路径。</summary>
        private const string FontProfileResourcePath = "Localization/LocalizationFontProfile";

        /// <summary>
        /// 字体缓存。Key = 语言代码（如 "zh-Hans"），Value = 对应的字体资产。
        /// 当语言改变或字体配置更新时应清空此缓存。
        /// </summary>
        private static readonly Dictionary<string, TMP_FontAsset> CachedFontsByLanguage =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>场景加载钩子是否已注册。</summary>
        private static bool _sceneHookRegistered;

        /// <summary>
        /// 清空字体缓存。
        /// 在语言改变或字体配置更新时调用。
        /// </summary>
        public static void InvalidateCache()
        {
            CachedFontsByLanguage.Clear();
        }

        /// <summary>
        /// 获取当前语言的首选字体。
        /// 流程：
        /// 1. 从语言服务获取当前语言代码
        /// 2. 检查缓存
        /// 3. 若缓存无效或缺失，从配置文件查询
        /// 4. 缓存并返回
        /// </summary>
        /// <returns>当前语言的 TMP 字体资产</returns>
        /// <exception cref="InvalidOperationException">
        /// - 语言服务未初始化
        /// - 字体配置缺失或无效
        /// </exception>
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

            // 检查缓存
            if (CachedFontsByLanguage.TryGetValue(languageCode, out TMP_FontAsset cached) && IsFontAssetUsable(cached))
            {
                return cached;
            }

            // 从配置查询并缓存
            TMP_FontAsset resolved = ResolveConfiguredFont(languageCode);
            CachedFontsByLanguage[languageCode] = resolved;
            return resolved;
        }

        /// <summary>
        /// 为单个 TMP_Text 组件应用当前语言的字体。
        /// 同时设置材质和备选字体。
        /// </summary>
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

            // 也应用共享材质
            if (fontAsset.material != null && text.fontSharedMaterial != fontAsset.material)
            {
                text.fontSharedMaterial = fontAsset.material;
            }

            // 更新 Mesh 内间距
            text.UpdateMeshPadding();
        }

        /// <summary>
        /// 为 Transform 下的所有 TMP_Text 子组件应用字体。
        /// 包括禁用的组件。
        /// </summary>
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

        /// <summary>
        /// 为所有已加载场景中的 TMP_Text 组件应用字体。
        /// 同时注册场景加载钩子，以在新场景加载时自动应用。
        /// </summary>
        public static void ApplyToAllLoadedSceneTexts()
        {
            RegisterSceneHooks();

            // 查找所有已加载场景中的 Text
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

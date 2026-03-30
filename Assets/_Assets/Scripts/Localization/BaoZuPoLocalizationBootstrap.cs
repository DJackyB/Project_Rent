using System;
using Martian.Localization;
using UnityEngine;

namespace BaoZuPo.Localization
{
    public static class BaoZuPoLocalizationConstants
    {
        public const string ChineseLanguageCode = "zh-Hans";
        public const string EnglishLanguageCode = "en";
        public const string DefaultLanguageCode = ChineseLanguageCode;
        public const string UiTable = "UI";
        public const string CardsTable = "Cards";
        public const string PlayerPrefsKey = "Martian.Localization.SelectedLocale";
    }

    public static class BaoZuPoLocalizationBootstrap
    {
        private static bool _initialized;

        public static string CurrentLanguageCode
        {
            get
            {
                EnsureInitialized();
                return LocalizationServices.Language.CurrentLanguageCode;
            }
        }

        public static bool UseChinese => string.Equals(CurrentLanguageCode, BaoZuPoLocalizationConstants.ChineseLanguageCode, StringComparison.OrdinalIgnoreCase);
        public static bool UseEnglish => string.Equals(CurrentLanguageCode, BaoZuPoLocalizationConstants.EnglishLanguageCode, StringComparison.OrdinalIgnoreCase);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            LocalizationServices.Reset();
            InitializeLocalizationServices();
            LocalizationFontUtility.ApplyToAllLoadedSceneTexts();
            _initialized = true;
        }

        public static bool SetLanguage(string languageCode)
        {
            EnsureInitialized();
            bool changed = LocalizationServices.Language.SetLanguage(languageCode);
            if (changed)
            {
                LocalizationFontUtility.ApplyToAllLoadedSceneTexts();
            }

            return changed;
        }

        private static void InitializeLocalizationServices()
        {
            const string bootstrapTypeName = "Martian.Localization.Unity.UnityLocalizationBootstrap, Martian.Localization.Unity";
            Type bootstrapType = Type.GetType(bootstrapTypeName, false);
            if (bootstrapType == null || !typeof(ILocalizationBootstrap).IsAssignableFrom(bootstrapType))
            {
                LocalizationServices.SetLanguageService(new NullLanguageService(
                    BaoZuPoLocalizationConstants.DefaultLanguageCode,
                    new[]
                    {
                        BaoZuPoLocalizationConstants.ChineseLanguageCode,
                        BaoZuPoLocalizationConstants.EnglishLanguageCode
                    }));
                LocalizationServices.SetTextService(NullLocalizedTextService.Instance);
                Debug.LogWarning(
                    "[BaoZuPoLocalizationBootstrap] Unity localization bridge is unavailable. " +
                    "Falling back to in-project text service. This is only intended for the bridge-absent case.");
                return;
            }

            try
            {
                var bootstrap = (ILocalizationBootstrap)Activator.CreateInstance(
                    bootstrapType,
                    BaoZuPoLocalizationConstants.DefaultLanguageCode,
                    BaoZuPoLocalizationConstants.PlayerPrefsKey);

                bootstrap.Initialize();
                LocalizationServices.SetBootstrap(bootstrap);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "[BaoZuPoLocalizationBootstrap] Failed to initialize Unity localization bridge. " +
                    "Fix the localization package/settings configuration instead of relying on config fallback.",
                    exception);
            }
        }
    }
}

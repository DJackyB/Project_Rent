using System;
using UnityEngine;

namespace BaoZuPo.UI
{
    public static class LocalizationManager
    {
        private const string PlayerPrefsKey = "BaoZuPo.Language";

        private static bool _initialized;
        private static AppLanguage _currentLanguage;

        public static event Action LanguageChanged;

        public static AppLanguage CurrentLanguage
        {
            get
            {
                EnsureInitialized();
                return _currentLanguage;
            }
        }

        public static bool UseChinese => CurrentLanguage == AppLanguage.Chinese;

        public static void SetLanguage(AppLanguage language)
        {
            EnsureInitialized();
            if (_currentLanguage == language)
            {
                return;
            }

            _currentLanguage = language;
            PlayerPrefs.SetInt(PlayerPrefsKey, (int)language);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                _currentLanguage = (AppLanguage)PlayerPrefs.GetInt(PlayerPrefsKey, (int)ResolveDefaultLanguage());
            }
            else
            {
                _currentLanguage = ResolveDefaultLanguage();
            }

            _initialized = true;
        }

        private static AppLanguage ResolveDefaultLanguage()
        {
            return Application.systemLanguage switch
            {
                SystemLanguage.Chinese => AppLanguage.Chinese,
                SystemLanguage.ChineseSimplified => AppLanguage.Chinese,
                SystemLanguage.ChineseTraditional => AppLanguage.Chinese,
                _ => AppLanguage.English
            };
        }
    }
}

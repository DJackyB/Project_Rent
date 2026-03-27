using System;
using BaoZuPo.Save;
using UnityEngine;

namespace BaoZuPo.UI
{
    public static class LocalizationManager
    {
        internal const string PlayerPrefsKey = "BaoZuPo.Language";

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
            WriteLegacyLanguagePreference(language);

            if (!SettingsSaveFacade.Shared.Save(out var error))
            {
                Debug.LogWarning($"[LocalizationManager] Failed to save settings. {error}");
            }

            LanguageChanged?.Invoke();
        }

        public static SettingsSaveState CaptureSettingsState()
        {
            EnsureInitialized();
            return new SettingsSaveState
            {
                language = (int)_currentLanguage
            };
        }

        public static void ApplySettingsState(SettingsSaveState state)
        {
            if (state == null)
            {
                return;
            }

            var resolvedLanguage = (AppLanguage)state.language;
            bool changed = !_initialized || _currentLanguage != resolvedLanguage;

            _currentLanguage = resolvedLanguage;
            _initialized = true;
            WriteLegacyLanguagePreference(resolvedLanguage);

            if (changed)
            {
                LanguageChanged?.Invoke();
            }
        }

        public static void ClearLegacyLanguagePreference()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                PlayerPrefs.DeleteKey(PlayerPrefsKey);
                PlayerPrefs.Save();
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (SettingsSaveFacade.Shared.TryReadState(out var settingsState))
            {
                _currentLanguage = (AppLanguage)settingsState.language;
                WriteLegacyLanguagePreference(_currentLanguage);
            }
            else if (PlayerPrefs.HasKey(PlayerPrefsKey))
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

        private static void WriteLegacyLanguagePreference(AppLanguage language)
        {
            PlayerPrefs.SetInt(PlayerPrefsKey, (int)language);
            PlayerPrefs.Save();
        }
    }
}

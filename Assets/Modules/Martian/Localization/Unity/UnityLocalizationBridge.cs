#if MARTIAN_LOCALIZATION_PACKAGE
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Martian.Localization.Unity
{
    internal static class UnityLocalizationRuntimeInstaller
    {
        private const string PlayerPrefsKey = "Martian.Localization.SelectedLocale";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            LocalizationServices.Reset();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            var bootstrap = new UnityLocalizationBootstrap(PlayerPrefsKey);
            LocalizationServices.SetBootstrap(bootstrap);
            bootstrap.Initialize();
        }
    }

    public sealed class UnityLocalizationBootstrap : ILocalizationBootstrap
    {
        private readonly string _playerPrefsKey;

        public UnityLocalizationBootstrap(string playerPrefsKey = "Martian.Localization.SelectedLocale")
        {
            _playerPrefsKey = string.IsNullOrWhiteSpace(playerPrefsKey) ? "Martian.Localization.SelectedLocale" : playerPrefsKey;
        }

        public bool IsInitialized { get; private set; }

        public LanguageSelectionResult Initialize()
        {
            if (IsInitialized)
            {
                return LocalizationServices.Language.LastSelection;
            }

            if (!LocalizationSettings.HasSettings)
            {
                throw new InvalidOperationException(
                    "[UnityLocalizationBootstrap] LocalizationSettings asset is missing. " +
                    "Configure Unity Localization (Window → Asset Management → Localization Settings).");
            }

            LocalizationSettings.InitializationOperation.WaitForCompletion();

            if (LocalizationSettings.ProjectLocale == null)
            {
                throw new InvalidOperationException(
                    "[UnityLocalizationBootstrap] ProjectLocale is not set in Localization Settings. " +
                    "Assign a Project Locale in Window → Asset Management → Localization Settings.");
            }

            string defaultLanguageCode = LocalizationSettings.ProjectLocale.Identifier.Code;
            var languageService = new UnityLanguageService(defaultLanguageCode, _playerPrefsKey);
            var textService = new UnityLocalizedTextService();

            languageService.Initialize();
            LocalizationServices.SetTextService(textService);
            LocalizationServices.SetLanguageService(languageService);

            IsInitialized = true;
            return languageService.LastSelection;
        }
    }

    public sealed class UnityLanguageService : ILanguageService
    {
        private readonly string _defaultLanguageCode;
        private readonly string _playerPrefsKey;
        private readonly List<string> _supportedLanguageCodes = new();
        private LanguageSelectionReason _pendingSelectionReason = LanguageSelectionReason.Default;

        public UnityLanguageService(string defaultLanguageCode, string playerPrefsKey)
        {
            _defaultLanguageCode = defaultLanguageCode;
            _playerPrefsKey = playerPrefsKey;
            CurrentLanguageCode = defaultLanguageCode;
            LastSelection = new LanguageSelectionResult(defaultLanguageCode, LanguageSelectionReason.Default);
        }

        public event Action<string> LanguageChanged;

        public bool IsAvailable => LocalizationSettings.HasSettings;
        public string CurrentLanguageCode { get; private set; }
        public IReadOnlyList<string> SupportedLanguageCodes => _supportedLanguageCodes;
        public LanguageSelectionResult LastSelection { get; private set; }

        public void Initialize()
        {
            RefreshSupportedLanguages();
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

            LanguageSelectionResult selection = ResolveStartupLanguage();
            _pendingSelectionReason = selection.Reason;
            if (!TryApplyLocale(selection.LanguageCode))
            {
                CurrentLanguageCode = selection.LanguageCode;
                LastSelection = selection;
                PersistLanguageCode(selection.LanguageCode);
                _pendingSelectionReason = LanguageSelectionReason.Default;
                return;
            }

            CurrentLanguageCode = selection.LanguageCode;
            LastSelection = selection;
            _pendingSelectionReason = LanguageSelectionReason.Default;
        }

        public bool SetLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return false;
            }

            if (string.Equals(CurrentLanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            _pendingSelectionReason = LanguageSelectionReason.ExplicitChange;
            if (!TryApplyLocale(languageCode))
            {
                _pendingSelectionReason = LanguageSelectionReason.Default;
                return false;
            }

            CurrentLanguageCode = languageCode;
            LastSelection = new LanguageSelectionResult(languageCode, LanguageSelectionReason.ExplicitChange);
            return true;
        }

        private void RefreshSupportedLanguages()
        {
            _supportedLanguageCodes.Clear();

            if (LocalizationSettings.AvailableLocales == null)
            {
                throw new InvalidOperationException(
                    "[UnityLanguageService] AvailableLocales is missing from LocalizationSettings. " +
                    "Configure supported locales explicitly.");
            }

            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            {
                if (locale == null)
                {
                    continue;
                }

                string code = locale.Identifier.Code;
                if (!string.IsNullOrWhiteSpace(code) && !_supportedLanguageCodes.Contains(code))
                {
                    _supportedLanguageCodes.Add(code);
                }
            }

            if (_supportedLanguageCodes.Count == 0)
            {
                throw new InvalidOperationException(
                    "[UnityLanguageService] No locales are configured. Add at least one supported locale.");
            }
        }

        private bool TryApplyLocale(string languageCode)
        {
            if (LocalizationSettings.AvailableLocales == null)
            {
                return false;
            }

            Locale locale = LocalizationSettings.AvailableLocales.Locales.FirstOrDefault(candidate =>
                candidate != null && string.Equals(candidate.Identifier.Code, languageCode, StringComparison.OrdinalIgnoreCase));

            if (locale == null)
            {
                return false;
            }

            LocalizationSettings.SelectedLocale = locale;
            return true;
        }

        private LanguageSelectionResult ResolveStartupLanguage()
        {
            if (PlayerPrefs.HasKey(_playerPrefsKey))
            {
                string persisted = PlayerPrefs.GetString(_playerPrefsKey);
                if (IsSupported(persisted))
                {
                    return new LanguageSelectionResult(persisted, LanguageSelectionReason.PersistedSelection);
                }
            }

            string systemLanguageCode = ResolveSystemLanguageCode();
            if (IsSupported(systemLanguageCode))
            {
                return new LanguageSelectionResult(systemLanguageCode, LanguageSelectionReason.SystemLanguage);
            }

            if (IsSupported(_defaultLanguageCode))
            {
                return new LanguageSelectionResult(_defaultLanguageCode, LanguageSelectionReason.Default);
            }

            return new LanguageSelectionResult(_supportedLanguageCodes[0], LanguageSelectionReason.Default);
        }

        private static string ResolveSystemLanguageCode()
        {
            return Application.systemLanguage switch
            {
                SystemLanguage.Chinese => "zh-Hans",
                SystemLanguage.ChineseSimplified => "zh-Hans",
                SystemLanguage.ChineseTraditional => "zh-Hans",
                _ => "en"
            };
        }

        private bool IsSupported(string languageCode)
        {
            return !string.IsNullOrWhiteSpace(languageCode)
                && _supportedLanguageCodes.Any(code => string.Equals(code, languageCode, StringComparison.OrdinalIgnoreCase));
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            string languageCode = locale != null ? locale.Identifier.Code : _defaultLanguageCode;
            CurrentLanguageCode = string.IsNullOrWhiteSpace(languageCode) ? _defaultLanguageCode : languageCode;
            LanguageSelectionReason reason = _pendingSelectionReason;
            if (reason == LanguageSelectionReason.Default && !string.Equals(LastSelection.LanguageCode, CurrentLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                reason = LanguageSelectionReason.ExplicitChange;
            }

            LastSelection = new LanguageSelectionResult(CurrentLanguageCode, reason);
            _pendingSelectionReason = LanguageSelectionReason.Default;
            PersistLanguageCode(CurrentLanguageCode);
            LanguageChanged?.Invoke(CurrentLanguageCode);
        }

        private void PersistLanguageCode(string languageCode)
        {
            PlayerPrefs.SetString(_playerPrefsKey, languageCode);
            PlayerPrefs.Save();
        }
    }

    public sealed class UnityLocalizedTextService : ILocalizedTextService
    {
        public bool IsAvailable => LocalizationSettings.HasSettings;

        public string Resolve(LocalizationTextRef textRef, params object[] arguments)
        {
            if (!LocalizationSettings.HasSettings)
            {
                return NullLocalizedTextService.Instance.Resolve(textRef, arguments);
            }

            if (string.IsNullOrWhiteSpace(textRef.Table) || string.IsNullOrWhiteSpace(textRef.Entry))
            {
                return NullLocalizedTextService.Instance.Resolve(textRef, arguments);
            }

            try
            {
                StringTable table = LocalizationSettings.StringDatabase.GetTable(textRef.Table);
                if (table == null)
                {
                    return NullLocalizedTextService.Instance.Resolve(textRef, arguments);
                }

                if (table.GetEntry(textRef.Entry) == null)
                {
                    return NullLocalizedTextService.Instance.Resolve(textRef, arguments);
                }

                var localizedString = new LocalizedString(textRef.Table, textRef.Entry)
                {
                    Arguments = arguments
                };

                return localizedString.GetLocalizedString();
            }
            catch
            {
                return NullLocalizedTextService.Instance.Resolve(textRef, arguments);
            }
        }
    }
}
#endif

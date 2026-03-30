using System;
using System.Collections.Generic;
using System.Globalization;

namespace Martian.Localization
{
    public static class LocalizationServices
    {
        private static ILanguageService _language = new NullLanguageService("zh-Hans");
        private static ILocalizedTextService _text = NullLocalizedTextService.Instance;
        private static ILocalizationBootstrap _bootstrap;
        private static LocalizationFontProfile _fontProfile;

        public static ILanguageService Language => _language;
        public static ILocalizedTextService Text => _text;
        public static ILocalizationBootstrap Bootstrap => _bootstrap;
        public static LocalizationFontProfile FontProfile => _fontProfile;

        public static void SetLanguageService(ILanguageService languageService)
        {
            _language = languageService ?? new NullLanguageService("zh-Hans");
            LocalizationFontUtility.InvalidateCache();
        }

        public static void SetTextService(ILocalizedTextService textService)
        {
            _text = textService ?? NullLocalizedTextService.Instance;
        }

        public static void SetBootstrap(ILocalizationBootstrap bootstrap)
        {
            _bootstrap = bootstrap;
        }

        public static void SetFontProfile(LocalizationFontProfile fontProfile)
        {
            _fontProfile = fontProfile;
            LocalizationFontUtility.InvalidateCache();
        }

        public static string Resolve(LocalizationTextRef textRef, params object[] arguments)
        {
            return _text.Resolve(textRef, arguments);
        }

        public static void Reset()
        {
            _bootstrap = null;
            _fontProfile = null;
            _language = new NullLanguageService("zh-Hans");
            _text = NullLocalizedTextService.Instance;
            LocalizationFontUtility.InvalidateCache();
        }
    }

    public sealed class NullLanguageService : ILanguageService
    {
        private readonly List<string> _supportedLanguageCodes = new();
        private string _currentLanguageCode;

        public NullLanguageService(string defaultLanguageCode, IEnumerable<string> supportedLanguageCodes = null)
        {
            _currentLanguageCode = string.IsNullOrWhiteSpace(defaultLanguageCode) ? "zh-Hans" : defaultLanguageCode;
            if (supportedLanguageCodes != null)
            {
                _supportedLanguageCodes.AddRange(supportedLanguageCodes);
            }

            if (_supportedLanguageCodes.Count == 0)
            {
                _supportedLanguageCodes.Add(_currentLanguageCode);
            }

            LastSelection = new LanguageSelectionResult(_currentLanguageCode, LanguageSelectionReason.FallbackService);
        }

        public event Action<string> LanguageChanged;

        public bool IsAvailable => false;
        public string CurrentLanguageCode => _currentLanguageCode;
        public IReadOnlyList<string> SupportedLanguageCodes => _supportedLanguageCodes;
        public LanguageSelectionResult LastSelection { get; private set; }

        public bool SetLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return false;
            }

            if (string.Equals(_currentLanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            _currentLanguageCode = languageCode;
            if (!_supportedLanguageCodes.Contains(languageCode))
            {
                _supportedLanguageCodes.Add(languageCode);
            }

            LastSelection = new LanguageSelectionResult(_currentLanguageCode, LanguageSelectionReason.ExplicitChange);
            LanguageChanged?.Invoke(_currentLanguageCode);
            return true;
        }
    }

    public sealed class NullLocalizedTextService : ILocalizedTextService
    {
        public static readonly NullLocalizedTextService Instance = new();

        private NullLocalizedTextService()
        {
        }

        public bool IsAvailable => false;

        public string Resolve(LocalizationTextRef textRef, params object[] arguments)
        {
            string text = textRef.FallbackPolicy switch
            {
                LocalizationFallbackPolicy.ReturnKey => !string.IsNullOrWhiteSpace(textRef.Entry) ? textRef.Entry : textRef.Fallback,
                LocalizationFallbackPolicy.ReturnEmpty => string.Empty,
                _ => textRef.Fallback
            };

            if (string.IsNullOrEmpty(text) || arguments == null || arguments.Length == 0)
            {
                return text ?? string.Empty;
            }

            try
            {
                return string.Format(CultureInfo.InvariantCulture, text, arguments);
            }
            catch (FormatException)
            {
                return text;
            }
        }
    }
}

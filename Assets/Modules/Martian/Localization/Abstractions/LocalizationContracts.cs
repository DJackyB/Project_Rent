using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martian.Localization
{
    public enum LocalizationFallbackPolicy
    {
        UseFallback = 0,
        ReturnKey = 1,
        ReturnEmpty = 2
    }

    public enum LanguageSelectionReason
    {
        Default = 0,
        PersistedSelection = 1,
        SystemLanguage = 2,
        ExplicitChange = 3,
        FallbackService = 4
    }

    [Serializable]
    public readonly struct LanguageSelectionResult
    {
        public LanguageSelectionResult(string languageCode, LanguageSelectionReason reason)
        {
            LanguageCode = languageCode ?? string.Empty;
            Reason = reason;
        }

        public string LanguageCode { get; }
        public LanguageSelectionReason Reason { get; }
    }

    [Serializable]
    public struct LocalizationTextRef
    {
        public LocalizationTextRef(string table, string entry, string fallback, LocalizationFallbackPolicy fallbackPolicy = LocalizationFallbackPolicy.UseFallback)
        {
            Table = table;
            Entry = entry;
            Fallback = fallback;
            FallbackPolicy = fallbackPolicy;
        }

        public string Table;
        public string Entry;
        [TextArea(1, 3)] public string Fallback;
        public LocalizationFallbackPolicy FallbackPolicy;
    }

    public interface ILanguageService
    {
        event Action<string> LanguageChanged;

        bool IsAvailable { get; }
        string CurrentLanguageCode { get; }
        IReadOnlyList<string> SupportedLanguageCodes { get; }
        LanguageSelectionResult LastSelection { get; }

        bool SetLanguage(string languageCode);
    }

    public interface ILocalizedTextService
    {
        bool IsAvailable { get; }
        string Resolve(LocalizationTextRef textRef, params object[] arguments);
    }

    public interface ILocalizationBootstrap
    {
        bool IsInitialized { get; }
        LanguageSelectionResult Initialize();
    }
}

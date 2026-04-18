using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Martian.Localization
{
    [CreateAssetMenu(fileName = "LocalizationFontProfile", menuName = "Martian/Localization/Font Profile")]
    public class LocalizationFontProfile : ScriptableObject
    {
        [Serializable]
        public sealed class LanguageFontMapping
        {
            public string languageCode = "zh-Hans";
            public TMP_FontAsset fontAsset;
            public List<TMP_FontAsset> fallbackFontAssets = new();
        }

        [SerializeField] private List<LanguageFontMapping> mappings = new();
        public IReadOnlyList<LanguageFontMapping> Mappings => mappings;

        public bool TryGetMapping(string languageCode, out LanguageFontMapping mapping)
        {
            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                for (int i = 0; i < mappings.Count; i++)
                {
                    if (mappings[i] != null && string.Equals(mappings[i].languageCode, languageCode, StringComparison.OrdinalIgnoreCase))
                    {
                        mapping = mappings[i];
                        return true;
                    }
                }
            }

            mapping = null;
            return false;
        }

        public LanguageFontMapping GetRequiredMapping(string languageCode)
        {
            if (TryGetMapping(languageCode, out var mapping) && mapping != null)
            {
                return mapping;
            }

            throw new InvalidOperationException(
                $"[LocalizationFontProfile] Missing font mapping for language '{languageCode}'. " +
                "Configure an explicit mapping in LocalizationFontProfile.");
        }
    }
}

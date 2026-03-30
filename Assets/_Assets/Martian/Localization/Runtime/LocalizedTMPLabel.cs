using TMPro;
using UnityEngine;

namespace Martian.Localization
{
    [DisallowMultipleComponent]
    public class LocalizedTMPLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private string table = "UI";
        [SerializeField] private string entry;
        [TextArea(1, 3)]
        [SerializeField] private string fallback;
        [SerializeField] private LocalizationFallbackPolicy fallbackPolicy = LocalizationFallbackPolicy.UseFallback;

        private void Awake()
        {
            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }
        }

        private void OnEnable()
        {
            LocalizationServices.Language.LanguageChanged += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationServices.Language.LanguageChanged -= OnLanguageChanged;
        }

        public void Refresh()
        {
            if (targetText == null)
            {
                return;
            }

            targetText.text = LocalizationServices.Resolve(new LocalizationTextRef(table, entry, fallback, fallbackPolicy));
            LocalizationFontUtility.ApplyToText(targetText);
        }

        private void OnLanguageChanged(string _)
        {
            Refresh();
        }
    }
}

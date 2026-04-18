using TMPro;
using UnityEngine;

namespace Martian.Localization
{
    [DisallowMultipleComponent]
    public class LocalizedFontApplier : MonoBehaviour
    {
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private bool includeChildren = true;

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
            if (includeChildren)
            {
                LocalizationFontUtility.ApplyToChildren(transform);
                return;
            }

            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }

            LocalizationFontUtility.ApplyToText(targetText);
        }

        private void OnLanguageChanged(string _)
        {
            Refresh();
        }
    }
}

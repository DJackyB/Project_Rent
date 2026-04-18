using TMPro;
using UnityEngine;

namespace Martian.Localization
{
    [DisallowMultipleComponent]
    public class LocalizedTMPInputPlaceholder : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private string table = "UI";
        [SerializeField] private string entry;
        [TextArea(1, 3)]
        [SerializeField] private string fallback;

        private void Awake()
        {
            if (inputField == null)
            {
                inputField = GetComponent<TMP_InputField>();
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
            if (inputField == null)
            {
                return;
            }

            TMP_Text placeholder = inputField.placeholder as TMP_Text;
            if (placeholder == null)
            {
                return;
            }

            placeholder.text = LocalizationServices.Resolve(new LocalizationTextRef(table, entry, fallback));
            LocalizationFontUtility.ApplyToText(placeholder);
        }

        private void OnLanguageChanged(string _)
        {
            Refresh();
        }
    }
}

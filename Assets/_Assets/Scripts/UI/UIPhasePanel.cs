using BaoZuPo.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    public class UIPhasePanel : MonoBehaviour
    {
        [Header("\u53ef\u9009\u573a\u666f\u5f15\u7528")]
        public TextMeshProUGUI phaseText;
        public Button endTurnButton;
        public TextMeshProUGUI buttonText;

        private void OnEnable()
        {
            EnsureRuntimeLayout();
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }
        }

        private void OnDisable()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
            }
        }

        public void UpdatePhase(string phaseName)
        {
            EnsureRuntimeLayout();
            UIFontCatalog.ApplyToText(phaseText);
            UIFontCatalog.ApplyToText(buttonText);

            if (phaseText != null)
            {
                phaseText.text = UIStrings.PhaseName(phaseName);
            }

            bool isAction = phaseName == "Action";
            if (endTurnButton != null)
            {
                endTurnButton.interactable = isAction;
            }

            if (buttonText != null)
            {
                buttonText.text = isAction ? UIStrings.EndTurnButton : UIStrings.WaitingButton;
            }
        }

        private void OnEndTurnClicked()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.EndActionPhase();
            }
        }

        private void EnsureRuntimeLayout()
        {
            var rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                return;
            }

            if (endTurnButton == null)
            {
                endTurnButton = CreateRuntimeButton(rootCanvas.transform);
                buttonText = endTurnButton.GetComponentInChildren<TextMeshProUGUI>();
            }

            endTurnButton.transform.SetParent(rootCanvas.transform, false);

            var rect = endTurnButton.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-24f, 24f);
                rect.sizeDelta = new Vector2(180f, 56f);
            }

            var image = endTurnButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.16f, 0.35f, 0.19f, 0.94f);
            }

            UIFontCatalog.ApplyToText(phaseText);
            UIFontCatalog.ApplyToText(buttonText);
        }

        private static Button CreateRuntimeButton(Transform parent)
        {
            var buttonObject = new GameObject("EndTurnButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.sprite = UIRuntimeSpriteUtility.GetWhiteSprite();
            image.type = Image.Type.Sliced;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = UIFontCatalog.GetPreferredFontAsset();
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            return buttonObject.GetComponent<Button>();
        }
    }
}

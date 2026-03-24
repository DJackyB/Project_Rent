using BaoZuPo.Card;
using BaoZuPo.UI.Common.Hover;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    public class CardHoverPreviewPresenter : MonoBehaviour, IHoverPreviewPresenter
    {
        private RectTransform _root;
        private GameObject _previewInstance;

        public RectTransform Root => _root;

        public void Show(HoverPreviewRequest request)
        {
            Hide();

            if (request == null || request.Source == null)
            {
                return;
            }

            var card = request.Source.HoverPayload as CardInstance;
            var sourceObject = request.Source.HoverSourceObject;
            if (card == null || sourceObject == null)
            {
                return;
            }

            _previewInstance = Instantiate(sourceObject, transform, false);
            _previewInstance.name = $"{sourceObject.name}_HoverPreview";
            _root = _previewInstance.transform as RectTransform;

            DisableInteraction(_previewInstance);

            var cardView = _previewInstance.GetComponent<UICardView>();
            if (cardView != null)
            {
                cardView.Setup(card, CardViewContext.HoverPreview, null);
                cardView.SetSelected(false);
            }

            ApplyPreviewSizing();
            _previewInstance.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
        }

        public void Hide()
        {
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }

            _root = null;
        }

        private static void DisableInteraction(GameObject previewObject)
        {
            foreach (var button in previewObject.GetComponentsInChildren<Button>(true))
            {
                button.interactable = false;
            }

            foreach (var layoutElement in previewObject.GetComponentsInChildren<LayoutElement>(true))
            {
                layoutElement.enabled = false;
            }

            foreach (var equipmentView in previewObject.GetComponentsInChildren<UIEquipmentCardView>(true))
            {
                equipmentView.enabled = false;
            }

            foreach (var trigger in previewObject.GetComponentsInChildren<HoverPreviewTrigger>(true))
            {
                trigger.enabled = false;
            }

            var canvasGroup = previewObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = previewObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void ApplyPreviewSizing()
        {
            if (_root == null)
            {
                return;
            }

            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            _root.sizeDelta = new Vector2(260f, 360f);

            var layoutElement = _root.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.enabled = false;
            }
        }
    }
}

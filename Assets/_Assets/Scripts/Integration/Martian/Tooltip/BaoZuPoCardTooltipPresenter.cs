using BaoZuPo.Card;
using BaoZuPo.UI.Common.Drag;
using BaoZuPo.UI;
using Martian.Tooltip;
using Martian.Tooltip.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.Integration.Martian.Tooltip
{
    public sealed class BaoZuPoCardTooltipPresenter : MonoBehaviour, ITooltipPresenter
    {
        [SerializeField] private Vector2 previewSize = new Vector2(260f, 360f);

        private RectTransform _root;
        private GameObject _previewInstance;

        public RectTransform Root => _root;

        public void Show(TooltipRequest request)
        {
            Hide();

            if (request == null || request.Content == null || request.Content.ContentId != BaoZuPoTooltipContentIds.CardPreview)
            {
                return;
            }

            var sourceObject = request.Anchor != null ? request.Anchor.gameObject : null;
            if (sourceObject == null)
            {
                return;
            }

            _previewInstance = Instantiate(sourceObject, transform, false);
            _previewInstance.name = $"{sourceObject.name}_TooltipPreview";
            _root = _previewInstance.transform as RectTransform;

            ConfigureClonedCardView(request.Content.Payload as CardInstance);
            DisableInteraction(_previewInstance);
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

        private void ConfigureClonedCardView(CardInstance card)
        {
            if (_previewInstance == null || card == null)
            {
                return;
            }

            var cardView = _previewInstance.GetComponent<UICardView>();
            if (cardView == null)
            {
                return;
            }

            cardView.Setup(card, CardViewContext.TooltipPreview, null);
            cardView.SetSelected(false);
        }

        private static void DisableInteraction(GameObject previewObject)
        {
            foreach (var graphic in previewObject.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

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

            foreach (var trigger in previewObject.GetComponentsInChildren<TooltipTrigger>(true))
            {
                trigger.enabled = false;
            }

            foreach (var dragHandler in previewObject.GetComponentsInChildren<UICardDragHandler>(true))
            {
                dragHandler.enabled = false;
            }

            var canvasGroup = previewObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                return;
            }

            Debug.LogError(
                "[CardTooltipPresenter] Card prefab requires CanvasGroup for tooltip previews. " +
                "Please configure Card.prefab instead of relying on AddComponent.",
                previewObject);
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
            _root.sizeDelta = previewSize;

            var layoutElement = _root.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.enabled = false;
            }
        }
    }

    internal sealed class BaoZuPoCardTooltipPresenterFactory : ITooltipPresenterFactory
    {
        public bool CanPresent(TooltipContent content)
        {
            return content != null
                && content.ContentId == BaoZuPoTooltipContentIds.CardPreview
                && content.Payload is CardInstance;
        }

        public ITooltipPresenter Create(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var presenterObject = new GameObject(nameof(BaoZuPoCardTooltipPresenter), typeof(RectTransform), typeof(BaoZuPoCardTooltipPresenter));
            presenterObject.transform.SetParent(parent, false);
            return presenterObject.GetComponent<BaoZuPoCardTooltipPresenter>();
        }
    }

    public static class BaoZuPoCardTooltipPresenterRegistration
    {
        private static readonly ITooltipPresenterFactory Factory = new BaoZuPoCardTooltipPresenterFactory();
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            TooltipPresenterRegistry.Register(Factory);
            _installed = true;
        }
    }
}

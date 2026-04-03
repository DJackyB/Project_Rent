using BaoZuPo.Card;
using BaoZuPo.UI.Common.Drag;
using BaoZuPo.UI.Common.Hover;
using Martian.Tooltip;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 卡牌悬停预览展示器，实例化卡牌对象并配置为预览模式。
    /// 克隆源卡牌对象，禁用所有交互（按钮、布局、拖拽、提示）。
    /// 应用预览专用的大小和显示方式。
    /// </summary>
    public class CardHoverPreviewPresenter : MonoBehaviour, IHoverPreviewPresenter
    {
        [SerializeField] private Vector2 previewSize = new Vector2(260f, 360f);

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

            var cardView = _previewInstance.GetComponent<UICardView>();
            if (cardView != null)
            {
                cardView.Setup(card, CardViewContext.TooltipPreview, null);
                cardView.SetSelected(false);
            }

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
                "[CardHoverPreviewPresenter] Card prefab requires CanvasGroup for hover previews. " +
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
}

using System;
using System.Reflection;
using BaoZuPo.UI.Common.Tooltip;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI.Common.Tooltip.Runtime
{
    public class CardTooltipPresenter : MonoBehaviour, ITooltipPresenter
    {
        [SerializeField] private Vector2 previewSize = new Vector2(260f, 360f);

        private RectTransform _root;
        private GameObject _previewInstance;

        public RectTransform Root => _root;

        public void Show(TooltipRequest request)
        {
            Hide();

            if (request == null || request.Content == null || request.Content.Kind != TooltipContentKind.Card)
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

            ConfigureClonedCardView(request.Content.Payload);
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

        private void ConfigureClonedCardView(object payload)
        {
            if (_previewInstance == null || payload == null)
            {
                return;
            }

            var cardView = ResolveBehaviour(_previewInstance, "UICardView");
            if (cardView == null)
            {
                return;
            }

            var cardViewType = cardView.GetType();
            var cardViewContextType = cardViewType.Assembly.GetType("BaoZuPo.Card.CardViewContext");
            if (cardViewContextType == null)
            {
                return;
            }

            object tooltipPreviewValue = Enum.Parse(cardViewContextType, "TooltipPreview");
            MethodInfo[] methods = cardViewType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != "Setup")
                {
                    continue;
                }

                var parameters = methods[i].GetParameters();
                if (parameters.Length != 3)
                {
                    continue;
                }

                methods[i].Invoke(cardView, new[] { payload, tooltipPreviewValue, null });
                break;
            }

            var setSelected = cardViewType.GetMethod("SetSelected", BindingFlags.Public | BindingFlags.Instance);
            setSelected?.Invoke(cardView, new object[] { false });
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

            var behaviours = previewObject.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null)
                {
                    continue;
                }

                string typeName = behaviours[i].GetType().Name;
                if (typeName == nameof(TooltipTrigger)
                    || typeName == "UICardDragHandler"
                    || typeName == "UIEquipmentCardView")
                {
                    behaviours[i].enabled = false;
                }
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

        private static Behaviour ResolveBehaviour(GameObject root, string typeName)
        {
            if (root == null || string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            var behaviours = root.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null && behaviours[i].GetType().Name == typeName)
                {
                    return behaviours[i];
                }
            }

            return null;
        }
    }

    internal sealed class CardTooltipPresenterFactory : ITooltipPresenterFactory
    {
        public bool CanPresent(TooltipContent content)
        {
            return content != null && content.Kind == TooltipContentKind.Card && content.Payload != null;
        }

        public ITooltipPresenter Create(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var presenterObject = new GameObject("CardTooltipPresenter", typeof(RectTransform), typeof(CardTooltipPresenter));
            presenterObject.transform.SetParent(parent, false);
            return presenterObject.GetComponent<CardTooltipPresenter>();
        }
    }

    public static class CardTooltipPresenterRegistration
    {
        private static readonly ITooltipPresenterFactory Factory = new CardTooltipPresenterFactory();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            TooltipPresenterRegistry.Register(Factory);
        }
    }
}

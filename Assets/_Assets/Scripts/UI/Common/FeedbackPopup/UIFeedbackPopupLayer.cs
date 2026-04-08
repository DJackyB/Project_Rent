using System;
using Martian.Localization;
using TMPro;
using UnityEngine;

namespace BaoZuPo.UI.Common.FeedbackPopup
{
    [DisallowMultipleComponent]
    public sealed class UIFeedbackPopupLayer : MonoBehaviour
    {
        [SerializeField] private UIFeedbackPopupStyle defaultStyle = new();
        [SerializeField] private UIFeedbackPopupView popupPrefab;

        public static Action<TMP_Text> DefaultTextConfigurator { get; set; } = ApplyProjectLocalizedFont;

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _layerRect;

        public UIFeedbackPopupStyle DefaultStyle => defaultStyle;

        private void Awake()
        {
            EnsureInitialized();
        }

        public static UIFeedbackPopupLayer GetOrCreate(Canvas canvas)
        {
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                return null;
            }

            var existing = canvas.GetComponentInChildren<UIFeedbackPopupLayer>(true);
            if (existing != null)
            {
                return existing;
            }

            var layerObject = new GameObject("UIFeedbackPopupLayer", typeof(RectTransform), typeof(CanvasGroup), typeof(UIFeedbackPopupLayer));
            layerObject.transform.SetParent(canvas.transform, false);

            var layerRect = layerObject.GetComponent<RectTransform>();
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            layerRect.SetAsLastSibling();

            var canvasGroup = layerObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            return layerObject.GetComponent<UIFeedbackPopupLayer>();
        }

        public UIFeedbackPopupView Show(RectTransform anchor, string text, string category = UIFeedbackPopupCategory.Default, Vector2? screenOffset = null)
        {
            return Show(new UIFeedbackPopupRequest
            {
                Anchor = anchor,
                Text = text,
                Category = string.IsNullOrWhiteSpace(category) ? UIFeedbackPopupCategory.Default : category,
                ScreenOffset = screenOffset ?? new Vector2(0f, 72f),
                UseScreenCenterFallback = anchor == null
            });
        }

        public UIFeedbackPopupView Show(UIFeedbackPopupRequest request)
        {
            EnsureInitialized();
            if (_layerRect == null || request == null || string.IsNullOrWhiteSpace(request.Text))
            {
                request?.Completed?.Invoke();
                return null;
            }

            var popup = CreatePopupView();
            if (popup == null)
            {
                request.Completed?.Invoke();
                return null;
            }

            request.TextConfigurator ??= DefaultTextConfigurator;
            popup.Play(request, request.Style ?? defaultStyle, _canvas, _canvasRect);
            return popup;
        }

        [ContextMenu("Show Test Popup")]
        private void ShowTestPopup()
        {
            Show(null, "+100", UIFeedbackPopupCategory.Positive, Vector2.zero);
        }

        [ContextMenu("Show Test Popup (Long Hold)")]
        private void ShowLongHoldTestPopup()
        {
            var style = defaultStyle.Clone();
            style.HoldSeconds = 1.25f;
            Show(new UIFeedbackPopupRequest
            {
                Text = "Cost -50",
                Category = UIFeedbackPopupCategory.Negative,
                ScreenOffset = Vector2.zero,
                Style = style
            });
        }

        private UIFeedbackPopupView CreatePopupView()
        {
            UIFeedbackPopupView popup;
            if (popupPrefab != null)
            {
                popup = Instantiate(popupPrefab, _layerRect, false);
            }
            else
            {
                var popupObject = new GameObject("UIFeedbackPopup", typeof(RectTransform), typeof(CanvasGroup), typeof(UIFeedbackPopupView));
                popupObject.transform.SetParent(_layerRect, false);
                popup = popupObject.GetComponent<UIFeedbackPopupView>();
            }

            popup.transform.SetAsLastSibling();
            return popup;
        }

        private static void ApplyProjectLocalizedFont(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            try
            {
                LocalizationFontUtility.ApplyToText(text);
            }
            catch
            {
                // Keep the generic popup API usable even if project localization is not ready.
            }
        }

        private void EnsureInitialized()
        {
            if (_layerRect == null)
            {
                _layerRect = transform as RectTransform;
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_canvasRect == null && _canvas != null)
            {
                _canvasRect = _canvas.transform as RectTransform;
            }

            if (_layerRect != null)
            {
                _layerRect.anchorMin = Vector2.zero;
                _layerRect.anchorMax = Vector2.one;
                _layerRect.offsetMin = Vector2.zero;
                _layerRect.offsetMax = Vector2.zero;
            }
        }
    }
}

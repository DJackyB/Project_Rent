using System;
using BaoZuPo.UI.Common.FlyingNumber;
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

        /// <summary>
        /// 从 sourceAnchor 位置沿弧线飞向 targetAnchor，到达时触发 onArrived。
        /// 用于结算收益飞向金钱显示的"跳字"效果。支持多个实例并行。
        /// </summary>
        public UIFlyingNumberView ShowFlyTo(
            string text,
            Color color,
            float fontSize,
            RectTransform sourceAnchor,
            Vector2 sourceScreenOffset,
            RectTransform targetAnchor,
            Vector2 targetScreenOffset,
            Action onArrived)
        {
            EnsureInitialized();
            if (_layerRect == null || string.IsNullOrWhiteSpace(text))
            {
                onArrived?.Invoke();
                return null;
            }

            Vector2 sourceLocalPos = ResolveLocalPosition(sourceAnchor, sourceScreenOffset);
            Vector2 targetLocalPos = ResolveLocalPosition(targetAnchor, targetScreenOffset);

            float distance = Vector2.Distance(sourceLocalPos, targetLocalPos);
            float arcHeight = Mathf.Clamp(distance * 0.28f, 80f, 240f);
            float flySeconds = Mathf.Clamp(0.4f + distance / 2000f, 0.4f, 0.75f);

            var viewObject = new GameObject(
                "UIFlyingNumber",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(UIFlyingNumberView));
            viewObject.transform.SetParent(_layerRect, false);
            viewObject.transform.SetAsLastSibling();

            var view = viewObject.GetComponent<UIFlyingNumberView>();
            view.Play(
                sourceLocalPos,
                targetLocalPos,
                text,
                color,
                fontSize,
                flySeconds,
                arcHeight,
                holdAfterArrive: 0.05f,
                fadeOutSeconds: 0.16f,
                onArrived);
            return view;
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

        private Vector2 ResolveLocalPosition(RectTransform anchor, Vector2 screenOffset)
        {
            Camera canvasCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            Vector2 screenPoint;
            if (anchor != null)
            {
                Vector3 worldPoint = anchor.TransformPoint(anchor.rect.center);
                screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldPoint) + screenOffset;
            }
            else
            {
                screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + screenOffset;
            }

            if (_layerRect != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(_layerRect, screenPoint, canvasCamera, out var localPoint))
            {
                return localPoint;
            }

            return screenOffset;
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
                // Martian.Localization 未接线时走此兜底：直接从 Resources 加载中文字体。
                // 接线后 try 块成功，catch 不再执行。
                var font = UnityEngine.Resources.Load<TMPro.TMP_FontAsset>(
                    "Fonts/SourceHanSansSC/SourceHanSansSC-Regular SDF");
                if (font != null)
                {
                    text.font = font;
                }
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

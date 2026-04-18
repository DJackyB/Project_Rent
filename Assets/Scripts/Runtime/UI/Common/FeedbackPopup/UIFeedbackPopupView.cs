using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI.Common.FeedbackPopup
{
    [DisallowMultipleComponent]
    public sealed class UIFeedbackPopupView : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image accent;

        private Image _background;
        private TextMeshProUGUI _label;
        private Image _accent;
        private Tween _sequence;
        private UIFeedbackPopupRequest _request;

        public void Play(UIFeedbackPopupRequest request, UIFeedbackPopupStyle style, Canvas canvas, RectTransform canvasRect)
        {
            _request = request;
            style ??= new UIFeedbackPopupStyle();

            EnsureView(style);
            ApplyStyle(request, style);
            request?.AnchorFeedback?.Invoke(request.Anchor);

            Vector2 anchoredPosition = ResolveAnchoredPosition(request, canvas, canvasRect);
            Vector2 entryPosition = anchoredPosition + style.EntryOffset;
            Vector2 settlePosition = anchoredPosition + style.SettleOffset;
            Vector2 overshootPosition = Vector2.Lerp(entryPosition, settlePosition, 0.72f);
            Vector2 exitPosition = anchoredPosition + style.ExitOffset;

            _rect.anchoredPosition = entryPosition;
            _rect.localScale = Vector3.one * Mathf.Max(0.01f, style.EntryScale);
            _canvasGroup.alpha = 0f;

            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Append(_canvasGroup.DOFade(1f, Mathf.Max(0.01f, style.FadeInSeconds)).SetEase(Ease.OutQuad));
            sequence.Join(_rect.DOAnchorPos(overshootPosition, Mathf.Max(0.01f, style.FadeInSeconds)).SetEase(Ease.OutCubic));
            sequence.Join(_rect.DOScale(Mathf.Max(0.01f, style.OvershootScale), Mathf.Max(0.01f, style.FadeInSeconds + style.PopOvershootSeconds)).SetEase(Ease.OutBack));
            sequence.Append(_rect.DOScale(Mathf.Max(0.01f, style.SettledScale), Mathf.Max(0.01f, style.SettleSeconds)).SetEase(Ease.OutQuad));
            sequence.Join(_rect.DOAnchorPos(settlePosition, Mathf.Max(0.01f, style.SettleSeconds)).SetEase(Ease.OutQuad));
            sequence.AppendInterval(Mathf.Max(0f, style.HoldSeconds));
            sequence.Append(_canvasGroup.DOFade(0f, Mathf.Max(0.01f, style.FadeOutSeconds)).SetEase(Ease.InQuad));
            sequence.Join(_rect.DOAnchorPos(exitPosition, Mathf.Max(0.01f, style.FadeOutSeconds)).SetEase(Ease.InCubic));
            sequence.Join(_rect.DOScale(Mathf.Max(0.01f, style.ExitScale), Mathf.Max(0.01f, style.FadeOutSeconds)).SetEase(Ease.InQuad));
            sequence.OnComplete(Complete);
            _sequence = sequence;
        }

        private void OnDestroy()
        {
            _sequence?.Kill(false);
            _sequence = null;
        }

        private void EnsureView(UIFeedbackPopupStyle style)
        {
            _rect = transform as RectTransform;
            if (_rect == null)
            {
                _rect = gameObject.AddComponent<RectTransform>();
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _background = GetComponent<Image>();
            if (_background == null)
            {
                _background = background;
            }

            if (_background == null)
            {
                _background = gameObject.AddComponent<Image>();
            }
            background = _background;

            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = style.PanelSize;

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            _background.sprite = UIFeedbackPopupSpriteUtility.WhiteSprite;
            _background.type = Image.Type.Simple;
            _background.raycastTarget = false;

            if (_label == null)
            {
                _label = label;
            }

            if (_label == null)
            {
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(transform, false);

                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = style.PanelPadding;
                labelRect.offsetMax = style.PanelPadding * -1f;

                _label = labelObject.GetComponent<TextMeshProUGUI>();
                _label.alignment = TextAlignmentOptions.Center;
                _label.textWrappingMode = TextWrappingModes.NoWrap;
                _label.raycastTarget = false;
            }
            label = _label;

            if (_accent == null)
            {
                _accent = accent;
            }

            if (_accent == null)
            {
                var accentObject = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                accentObject.transform.SetParent(transform, false);

                var accentRect = accentObject.GetComponent<RectTransform>();
                accentRect.anchorMin = new Vector2(0f, 1f);
                accentRect.anchorMax = new Vector2(1f, 1f);
                accentRect.pivot = new Vector2(0.5f, 1f);
                accentRect.anchoredPosition = Vector2.zero;
                accentRect.sizeDelta = new Vector2(0f, 5f);

                _accent = accentObject.GetComponent<Image>();
                _accent.sprite = UIFeedbackPopupSpriteUtility.WhiteSprite;
                _accent.type = Image.Type.Simple;
                _accent.raycastTarget = false;
                _accent.transform.SetAsFirstSibling();
            }
            accent = _accent;
        }

        private void ApplyStyle(UIFeedbackPopupRequest request, UIFeedbackPopupStyle style)
        {
            _label.text = request.Text;
            _label.color = style.TextColor;
            _label.fontSize = request.IsFinal ? style.FinalFontSize : style.FontSize;
            _label.fontStyle = request.IsFinal ? FontStyles.Bold : FontStyles.Normal;
            request.TextConfigurator?.Invoke(_label);
            Color panelColor = style.ResolvePanelColor(request.Category, request.IsFinal);
            _background.color = panelColor;
            if (_accent != null)
            {
                _accent.color = Color.Lerp(panelColor, Color.white, 0.42f);
            }

            var preferred = _label.GetPreferredValues(request.Text, 720f, 0f);
            float width = Mathf.Max(style.PanelSize.x, preferred.x + style.PanelPadding.x * 2f);
            float height = Mathf.Max(style.PanelSize.y, preferred.y + style.PanelPadding.y * 2f);
            _rect.sizeDelta = new Vector2(width, height);
        }

        private static Vector2 ResolveAnchoredPosition(UIFeedbackPopupRequest request, Canvas canvas, RectTransform canvasRect)
        {
            Camera canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector2 screenOffset = request != null ? request.ScreenOffset : Vector2.zero;
            Vector2 screenPoint;

            if (request != null && request.Anchor != null)
            {
                Vector3 worldPoint = request.Anchor.TransformPoint(request.Anchor.rect.center);
                screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldPoint) + screenOffset;
            }
            else if (request == null || request.UseScreenCenterFallback)
            {
                screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + screenOffset;
            }
            else
            {
                screenPoint = screenOffset;
            }

            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvasCamera, out var localPoint))
            {
                return localPoint;
            }

            return screenOffset;
        }

        private void Complete()
        {
            _request?.Completed?.Invoke();
            _request = null;

            if (this != null && gameObject != null)
            {
                Destroy(gameObject);
            }
        }
    }

    internal static class UIFeedbackPopupSpriteUtility
    {
        private static Sprite _whiteSprite;

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null)
                {
                    return _whiteSprite;
                }

                var texture = Texture2D.whiteTexture;
                _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                return _whiteSprite;
            }
        }
    }
}

using DG.Tweening;
using Martian.Feedback;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Martian.Feedback.Runtime
{
    internal sealed class FeedbackPlaybackTrack : MonoBehaviour
    {
        private readonly System.Collections.Generic.Queue<FeedbackPlaybackRequest> _queue = new();

        private FeedbackRuntimeOptions _options = new();
        private FeedbackPlaybackRequest _currentRequest;
        private Sequence _activeSequence;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _panelRoot;
        private CanvasGroup _panelGroup;
        private Image _panelImage;
        private Image _glowImage;
        private Image _accentImage;
        private TextMeshProUGUI _label;
        private Func<TMP_FontAsset> _fontResolver;

        public event System.Action PlaybackCompleted;

        public bool IsBusy => _activeSequence != null || _currentRequest != null || _queue.Count > 0;

        public int PendingCount => _queue.Count + (_currentRequest != null ? 1 : 0);

        public void SetFontResolver(Func<TMP_FontAsset> fontResolver)
        {
            _fontResolver = fontResolver;
            ApplyResolvedFont();
        }

        public void Configure(Canvas canvas, RectTransform canvasRect, FeedbackRuntimeOptions options)
        {
            _canvas = canvas;
            _canvasRect = canvasRect;
            _options = options != null ? options.Clone() : new FeedbackRuntimeOptions();
            EnsureView();
        }

        public void Enqueue(FeedbackPlaybackRequest request)
        {
            if (request == null || request.Steps == null || request.Steps.Count == 0)
            {
                return;
            }

            EnsureView();
            _queue.Enqueue(CloneRequest(request));
            TryPlayNext();
        }

        public void Clear()
        {
            CancelHandle(_currentRequest);

            foreach (var request in _queue)
            {
                CancelHandle(request);
            }

            _queue.Clear();
            _currentRequest = null;

            if (_activeSequence != null)
            {
                _activeSequence.Kill(false);
                _activeSequence = null;
            }

            ResetView();
        }

        internal void CompleteCurrentForTesting()
        {
            if (_activeSequence != null)
            {
                _activeSequence.Kill(false);
                _activeSequence = null;
            }

            FinishCurrentRequest();
        }

        private void OnDisable()
        {
            Clear();
        }

        private void TryPlayNext()
        {
            if (_activeSequence != null)
            {
                return;
            }

            EnsureView();

            if (_queue.Count == 0)
            {
                ResetView();
                PlaybackCompleted?.Invoke();
                return;
            }

            _currentRequest = _queue.Dequeue();
            if (_currentRequest == null || _currentRequest.Steps == null || _currentRequest.Steps.Count == 0)
            {
                FinishCurrentRequest();
                return;
            }

            _activeSequence = BuildSequence(_currentRequest);
            if (_activeSequence == null)
            {
                FinishCurrentRequest();
                return;
            }

            _activeSequence.OnComplete(FinishCurrentRequest);
            _activeSequence.Play();
        }

        private Sequence BuildSequence(FeedbackPlaybackRequest request)
        {
            if (request == null || request.Steps == null || request.Steps.Count == 0)
            {
                return null;
            }

            int validStepCount = 0;
            for (int i = 0; i < request.Steps.Count; i++)
            {
                if (request.Steps[i] != null && !string.IsNullOrWhiteSpace(request.Steps[i].Text))
                {
                    validStepCount++;
                }
            }

            if (validStepCount == 0)
            {
                return null;
            }

            var canvasCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            var sequence = DOTween.Sequence().SetUpdate(true);
            int resolvedStepIndex = 0;

            for (int i = 0; i < request.Steps.Count; i++)
            {
                var step = request.Steps[i];
                if (step == null || string.IsNullOrWhiteSpace(step.Text))
                {
                    continue;
                }

                bool isFinalStep = resolvedStepIndex == validStepCount - 1;
                Vector2 anchoredPosition = ResolveAnchoredPosition(request, step, canvasCamera);
                Vector2 entryPosition = anchoredPosition + new Vector2(0f, isFinalStep ? -18f : -12f);
                Vector2 settlePosition = anchoredPosition + new Vector2(0f, isFinalStep ? 12f : 8f);
                Vector2 exitPosition = anchoredPosition + new Vector2(0f, isFinalStep ? 28f : 18f);
                float fadeInSeconds = Mathf.Max(0.01f, step.FadeInSeconds);
                float fadeOutSeconds = Mathf.Max(0.01f, step.FadeOutSeconds);
                float holdSeconds = Mathf.Max(0f, step.HoldSeconds);
                float baseScale = Mathf.Max(0.85f, step.Scale);
                float emphasizedScale = isFinalStep ? Mathf.Max(baseScale, 1.12f) : baseScale;
                float entryScale = emphasizedScale * (isFinalStep ? 0.92f : 0.95f);
                float exitScale = emphasizedScale * (isFinalStep ? 1.06f : 1.02f);

                sequence.AppendCallback(() => PrepareStep(step, isFinalStep, entryPosition, entryScale));
                sequence.Append(_panelGroup.DOFade(1f, fadeInSeconds));
                sequence.Join(_panelRoot.DOScale(emphasizedScale, fadeInSeconds).SetEase(isFinalStep ? Ease.OutBack : Ease.OutQuad));
                sequence.Join(_panelRoot.DOAnchorPos(settlePosition, fadeInSeconds).SetEase(Ease.OutCubic));
                sequence.AppendInterval(holdSeconds);
                sequence.Append(_panelGroup.DOFade(0f, fadeOutSeconds));
                sequence.Join(_panelRoot.DOScale(exitScale, fadeOutSeconds).SetEase(Ease.OutQuad));
                sequence.Join(_panelRoot.DOAnchorPos(exitPosition, fadeOutSeconds).SetEase(Ease.InCubic));

                if (!isFinalStep && request.GapSeconds > 0f)
                {
                    sequence.AppendInterval(request.GapSeconds);
                }

                resolvedStepIndex++;
            }

            return sequence;
        }

        private void PrepareStep(FeedbackPlaybackStep step, bool isFinalStep, Vector2 anchoredPosition, float entryScale)
        {
            if (_panelRoot == null || _panelGroup == null || _label == null || step == null)
            {
                return;
            }

            _panelRoot.anchoredPosition = anchoredPosition;
            _panelRoot.localScale = Vector3.one * entryScale;
            _panelRoot.gameObject.SetActive(true);
            _panelGroup.alpha = 0f;
            _label.text = step.Text;
            ApplyResolvedFont();
            _label.color = isFinalStep ? Color.Lerp(step.Color, Color.white, 0.18f) : Color.Lerp(step.Color, Color.white, 0.05f);
            _label.fontStyle = isFinalStep ? FontStyles.Bold : FontStyles.Normal;
            _label.fontSize = isFinalStep ? 27f : 24f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            if (_panelImage != null)
            {
                _panelImage.color = ResolvePanelColor(step, isFinalStep);
            }

            if (_glowImage != null)
            {
                _glowImage.color = ResolveGlowColor(step, isFinalStep);
            }

            if (_accentImage != null)
            {
                _accentImage.color = ResolveAccentColor(step, isFinalStep);
            }

            ApplySizing(step.Text);
        }

        private void FinishCurrentRequest()
        {
            CompleteHandle(_currentRequest);
            _currentRequest = null;

            if (_activeSequence != null)
            {
                _activeSequence.Kill(false);
                _activeSequence = null;
            }

            if (_queue.Count == 0)
            {
                ResetView();
                PlaybackCompleted?.Invoke();
                return;
            }

            TryPlayNext();
        }

        private void EnsureView()
        {
            if (_panelRoot == null)
            {
                _panelRoot = transform as RectTransform;
                if (_panelRoot == null)
                {
                    _panelRoot = gameObject.AddComponent<RectTransform>();
                }
            }

            if (_panelGroup == null)
            {
                _panelGroup = GetComponent<CanvasGroup>();
                if (_panelGroup == null)
                {
                    _panelGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (_panelImage == null)
            {
                _panelImage = GetComponent<Image>();
                if (_panelImage == null)
                {
                    _panelImage = gameObject.AddComponent<Image>();
                }
            }

            _panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRoot.pivot = new Vector2(0.5f, 0.5f);
            _panelRoot.sizeDelta = _options.PanelSize;

            _panelGroup.blocksRaycasts = false;
            _panelGroup.interactable = false;

            _panelImage.sprite = FeedbackSpriteUtility.WhiteSprite;
            _panelImage.type = Image.Type.Simple;
            _panelImage.color = _options.PanelColor;
            _panelImage.raycastTarget = false;

            if (_glowImage == null)
            {
                _glowImage = EnsureChildImage("Glow");
            }

            if (_accentImage == null)
            {
                _accentImage = EnsureChildImage("Accent");
            }

            if (_glowImage != null)
            {
                var glowRect = _glowImage.rectTransform;
                glowRect.anchorMin = Vector2.zero;
                glowRect.anchorMax = Vector2.one;
                glowRect.offsetMin = new Vector2(-12f, -12f);
                glowRect.offsetMax = new Vector2(12f, 12f);
                glowRect.SetAsFirstSibling();
                _glowImage.sprite = FeedbackSpriteUtility.WhiteSprite;
                _glowImage.type = Image.Type.Simple;
                _glowImage.raycastTarget = false;
                _glowImage.color = new Color(1f, 1f, 1f, 0f);
            }

            if (_accentImage != null)
            {
                var accentRect = _accentImage.rectTransform;
                accentRect.anchorMin = new Vector2(0f, 1f);
                accentRect.anchorMax = new Vector2(1f, 1f);
                accentRect.pivot = new Vector2(0.5f, 1f);
                accentRect.anchoredPosition = Vector2.zero;
                accentRect.sizeDelta = new Vector2(0f, 8f);
                _accentImage.sprite = FeedbackSpriteUtility.WhiteSprite;
                _accentImage.type = Image.Type.Simple;
                _accentImage.raycastTarget = false;
                _accentImage.color = new Color(1f, 1f, 1f, 0f);
            }

            if (_label == null)
            {
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(transform, false);

                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = _options.PanelPadding;
                labelRect.offsetMax = _options.PanelPadding * -1f;

                _label = labelObject.GetComponent<TextMeshProUGUI>();
                ApplyResolvedFont();

                _label.color = _options.TextColor;
                _label.fontSize = 24f;
                _label.alignment = TextAlignmentOptions.Center;
                _label.textWrappingMode = TextWrappingModes.NoWrap;
                _label.raycastTarget = false;
            }
        }

        private void ResetView()
        {
            if (_panelRoot != null)
            {
                _panelRoot.gameObject.SetActive(false);
                _panelRoot.localScale = Vector3.one;
            }

            if (_panelGroup != null)
            {
                _panelGroup.alpha = 0f;
            }
        }

        private Image EnsureChildImage(string childName)
        {
            var child = transform.Find(childName);
            GameObject childObject;
            if (child == null)
            {
                childObject = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                childObject.transform.SetParent(transform, false);
            }
            else
            {
                childObject = child.gameObject;
            }

            return childObject.GetComponent<Image>();
        }

        private void ApplyResolvedFont()
        {
            if (_label == null)
            {
                return;
            }

            TMP_FontAsset fontAsset = null;
            if (_fontResolver != null)
            {
                try
                {
                    fontAsset = _fontResolver.Invoke();
                }
                catch
                {
                    fontAsset = null;
                }
            }

            fontAsset ??= TMP_Settings.defaultFontAsset;
            if (fontAsset == null)
            {
                return;
            }

            if (_label.font != fontAsset)
            {
                _label.font = fontAsset;
            }

            if (fontAsset.material != null && _label.fontSharedMaterial != fontAsset.material)
            {
                _label.fontSharedMaterial = fontAsset.material;
            }

            _label.UpdateMeshPadding();
        }

        private void ApplySizing(string text)
        {
            if (_label == null || _panelRoot == null)
            {
                return;
            }

            string resolvedText = text ?? string.Empty;
            var preferred = _label.GetPreferredValues(resolvedText, 720f, 0f);
            float width = Mathf.Max(_options.PanelSize.x, preferred.x + _options.PanelPadding.x * 2f);
            float height = Mathf.Max(_options.PanelSize.y, preferred.y + _options.PanelPadding.y * 2f);
            _panelRoot.sizeDelta = new Vector2(width, height);
        }

        private Vector2 ResolveAnchoredPosition(FeedbackPlaybackRequest request, FeedbackPlaybackStep step, Camera canvasCamera)
        {
            Vector2 screenPoint = ResolveScreenPoint(request) + step.Offset;
            if (_canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, canvasCamera, out var localPoint))
            {
                return localPoint;
            }

            return step.Offset;
        }

        private Vector2 ResolveScreenPoint(FeedbackPlaybackRequest request)
        {
            Vector2 screenOffset = request != null ? request.ScreenOffset : Vector2.zero;

            if (request != null && request.Anchor != null)
            {
                return RectTransformUtility.WorldToScreenPoint(_canvas != null ? _canvas.worldCamera : null, request.Anchor.position) + screenOffset;
            }

            if (request == null || request.UseScreenCenterFallback)
            {
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + screenOffset;
            }

            return screenOffset;
        }

        private static FeedbackPlaybackRequest CloneRequest(FeedbackPlaybackRequest request)
        {
            if (request == null)
            {
                return null;
            }

            var clone = new FeedbackPlaybackRequest
            {
                DebugLabel = request.DebugLabel,
                LaneKey = request.LaneKey,
                TargetKey = request.TargetKey,
                TargetKind = request.TargetKind,
                Anchor = request.Anchor,
                UseScreenCenterFallback = request.UseScreenCenterFallback,
                ScreenOffset = request.ScreenOffset,
                GapSeconds = request.GapSeconds,
                Handle = request.Handle
            };

            if (request.Steps != null)
            {
                for (int i = 0; i < request.Steps.Count; i++)
                {
                    var step = request.Steps[i];
                    if (step == null)
                    {
                        continue;
                    }

                    clone.Steps.Add(new FeedbackPlaybackStep
                    {
                        Text = step.Text,
                        Color = step.Color,
                        HoldSeconds = step.HoldSeconds,
                        FadeInSeconds = step.FadeInSeconds,
                        FadeOutSeconds = step.FadeOutSeconds,
                        Scale = step.Scale,
                        Offset = step.Offset,
                        IsFinalStep = step.IsFinalStep,
                        Category = step.Category
                    });
                }
            }

            return clone;
        }

        private static void CompleteHandle(FeedbackPlaybackRequest request)
        {
            request?.Handle?.Complete();
        }

        private static void CancelHandle(FeedbackPlaybackRequest request)
        {
            request?.Handle?.Cancel();
        }

        private static Color ResolvePanelColor(FeedbackPlaybackStep step, bool isFinalStep)
        {
            Color baseColor = step != null ? step.Color : Color.white;
            Color darkPanel = new Color(0.07f, 0.1f, 0.16f, 0.88f);
            Color tinted = Color.Lerp(darkPanel, baseColor, isFinalStep ? 0.3f : 0.2f);
            tinted.a = isFinalStep ? 0.92f : 0.8f;
            return tinted;
        }

        private static Color ResolveGlowColor(FeedbackPlaybackStep step, bool isFinalStep)
        {
            Color baseColor = step != null ? step.Color : Color.white;
            baseColor.a = isFinalStep ? 0.22f : 0.14f;
            return baseColor;
        }

        private static Color ResolveAccentColor(FeedbackPlaybackStep step, bool isFinalStep)
        {
            Color baseColor = step != null ? step.Color : Color.white;
            baseColor.a = isFinalStep ? 0.96f : 0.84f;
            return baseColor;
        }
    }

    internal static class FeedbackSpriteUtility
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

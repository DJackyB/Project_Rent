using DG.Tweening;
using Martian.Feedback;
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
        private TextMeshProUGUI _label;

        public event System.Action PlaybackCompleted;

        public bool IsBusy => _activeSequence != null || _currentRequest != null || _queue.Count > 0;

        public int PendingCount => _queue.Count + (_currentRequest != null ? 1 : 0);

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
                float riseDistance = isFinalStep ? 24f : 18f;
                Vector2 targetPosition = anchoredPosition + new Vector2(0f, riseDistance);
                float fadeInSeconds = Mathf.Max(0.01f, step.FadeInSeconds);
                float fadeOutSeconds = Mathf.Max(0.01f, step.FadeOutSeconds);
                float holdSeconds = Mathf.Max(0f, step.HoldSeconds);
                float moveSeconds = Mathf.Max(0.01f, fadeInSeconds + holdSeconds);

                sequence.AppendCallback(() => PrepareStep(step, isFinalStep, anchoredPosition));
                sequence.Append(_panelGroup.DOFade(1f, fadeInSeconds));
                sequence.Join(_panelRoot.DOScale(step.Scale, fadeInSeconds));
                sequence.Join(_panelRoot.DOAnchorPos(targetPosition, moveSeconds).SetEase(Ease.OutQuad));
                sequence.AppendInterval(holdSeconds);
                sequence.Append(_panelGroup.DOFade(0f, fadeOutSeconds));

                if (!isFinalStep && request.GapSeconds > 0f)
                {
                    sequence.AppendInterval(request.GapSeconds);
                }

                resolvedStepIndex++;
            }

            return sequence;
        }

        private void PrepareStep(FeedbackPlaybackStep step, bool isFinalStep, Vector2 anchoredPosition)
        {
            if (_panelRoot == null || _panelGroup == null || _label == null || step == null)
            {
                return;
            }

            _panelRoot.anchoredPosition = anchoredPosition;
            _panelRoot.localScale = Vector3.one;
            _panelRoot.gameObject.SetActive(true);
            _panelGroup.alpha = 0f;
            _label.text = step.Text;
            _label.color = isFinalStep ? Color.Lerp(step.Color, Color.white, 0.16f) : step.Color;
            _label.fontStyle = isFinalStep ? FontStyles.Bold : FontStyles.Normal;
            _label.fontSize = isFinalStep ? 26f : 24f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            ApplySizing(step.Text);
        }

        private void FinishCurrentRequest()
        {
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
            _panelImage.type = Image.Type.Sliced;
            _panelImage.color = _options.PanelColor;
            _panelImage.raycastTarget = false;

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
                if (TMP_Settings.defaultFontAsset != null)
                {
                    _label.font = TMP_Settings.defaultFontAsset;
                }

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
                TargetKey = request.TargetKey,
                TargetKind = request.TargetKind,
                Anchor = request.Anchor,
                UseScreenCenterFallback = request.UseScreenCenterFallback,
                ScreenOffset = request.ScreenOffset,
                GapSeconds = request.GapSeconds
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

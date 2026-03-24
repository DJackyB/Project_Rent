using System.Collections.Generic;
using BaoZuPo.UI.Common.Animation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI.Common.Sequence
{
    public class UISequenceTextController : MonoBehaviour
    {
        public event System.Action PlaybackCompleted;

        [Header("Runtime Sequence UI")]
        [SerializeField] private int sortingOrder = 6000;
        [SerializeField] private Vector2 defaultSize = new Vector2(280f, 90f);
        [SerializeField] private Vector2 panelPadding = new Vector2(44f, 20f);
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.56f);
        [SerializeField] private Color defaultTextColor = Color.white;

        private readonly Queue<UISequencePlaybackRequest> _queue = new();
        private Sequence _activeSequence;
        private bool _isPlaying;

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _panelRoot;
        private CanvasGroup _panelGroup;
        private Image _panelImage;
        private TextMeshProUGUI _label;

        public bool IsPlaying => _isPlaying;

        public void Enqueue(UISequencePlaybackRequest request)
        {
            if (request == null || request.Steps == null || request.Steps.Count == 0)
            {
                return;
            }

            EnsureRuntimeView();
            _queue.Enqueue(request.Clone());
            TryPlayNext();
        }

        public void ClearQueue(bool hideImmediately = true)
        {
            _queue.Clear();

            if (hideImmediately)
            {
                HideImmediate();
            }
        }

        public void HideImmediate()
        {
            _queue.Clear();
            _isPlaying = false;

            if (_activeSequence != null)
            {
                _activeSequence.Kill(false);
                _activeSequence = null;
            }

            ResetView();
        }

        private void OnDisable()
        {
            HideImmediate();
        }

        private void TryPlayNext()
        {
            if (_isPlaying)
            {
                return;
            }

            EnsureRuntimeView();

            if (_queue.Count == 0)
            {
                ResetView();
                PlaybackCompleted?.Invoke();
                return;
            }

            var request = _queue.Dequeue();
            if (request == null || request.Steps == null || request.Steps.Count == 0)
            {
                TryPlayNext();
                return;
            }

            _isPlaying = true;

            _activeSequence = BuildRequestSequence(request);
            if (_activeSequence == null)
            {
                _isPlaying = false;
                TryPlayNext();
                return;
            }

            _activeSequence.OnComplete(() =>
            {
                _activeSequence = null;
                _isPlaying = false;
                TryPlayNext();
            });
            _activeSequence.Play();
        }

        private Sequence BuildRequestSequence(UISequencePlaybackRequest request)
        {
            if (request == null || request.Steps == null || request.Steps.Count == 0)
            {
                return null;
            }

            var sequence = DOTween.Sequence().SetUpdate(true);
            int validStepTotal = GetValidStepCount(request.Steps);
            int validStepCount = 0;

            for (int i = 0; i < request.Steps.Count; i++)
            {
                var step = request.Steps[i];
                if (step == null || string.IsNullOrWhiteSpace(step.Text))
                {
                    continue;
                }

                bool isFinalStep = validStepCount == validStepTotal - 1;
                Vector2 anchoredPosition = ResolveAnchoredPosition(request, step);
                sequence.AppendCallback(() => PrepareStepVisuals(step, isFinalStep));
                sequence.Append(UIAnimationTweenUtility.BuildFloatingStepSequence(
                    _panelRoot,
                    _panelGroup,
                    anchoredPosition,
                    step,
                    isFinalStep));

                if (!isFinalStep && request.GapSeconds > 0f)
                {
                    sequence.AppendInterval(Mathf.Max(0f, request.GapSeconds));
                }

                validStepCount++;
            }

            if (validStepCount == 0)
            {
                return null;
            }

            return sequence;
        }

        private void PrepareStepVisuals(UISequenceStep step, bool isFinalStep)
        {
            if (_label == null || step == null)
            {
                return;
            }

            _label.text = step.Text;
            _label.color = isFinalStep ? Color.Lerp(step.Color, Color.white, 0.16f) : step.Color;
            _label.alignment = TextAlignmentOptions.Center;
            _label.enableWordWrapping = false;
            _label.fontStyle = isFinalStep ? FontStyles.Bold : FontStyles.Normal;
            _label.fontSize = isFinalStep ? 26f : 24f;

            ApplySizing(step);
        }

        private void ResetView()
        {
            if (_activeSequence != null)
            {
                _activeSequence.Kill(false);
                _activeSequence = null;
            }

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

        private void EnsureRuntimeView()
        {
            EnsureCanvas();

            if (_panelRoot != null)
            {
                return;
            }

            var rootObject = new GameObject("SequenceBubble", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            rootObject.transform.SetParent(transform, false);

            _panelRoot = rootObject.GetComponent<RectTransform>();
            _panelGroup = rootObject.GetComponent<CanvasGroup>();
            _panelImage = rootObject.GetComponent<Image>();

            _panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRoot.pivot = new Vector2(0.5f, 0.5f);
            _panelRoot.sizeDelta = defaultSize;
            _panelRoot.gameObject.SetActive(false);

            _panelGroup.alpha = 0f;
            _panelGroup.interactable = false;
            _panelGroup.blocksRaycasts = false;

            _panelImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            _panelImage.type = Image.Type.Sliced;
            _panelImage.color = panelColor;
            _panelImage.raycastTarget = false;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(rootObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = panelPadding;
            labelRect.offsetMax = panelPadding * -1f;

            _label = labelObject.GetComponent<TextMeshProUGUI>();
            _label.font = TMP_Settings.defaultFontAsset != null
                ? TMP_Settings.defaultFontAsset
                : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            _label.color = defaultTextColor;
            _label.fontSize = 24f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.enableWordWrapping = false;
            _label.raycastTarget = false;
        }

        private void EnsureCanvas()
        {
            if (_canvas != null)
            {
                return;
            }

            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = sortingOrder;
            _canvasRect = _canvas.transform as RectTransform;

            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void ApplySizing(UISequenceStep step)
        {
            if (_label == null || _panelRoot == null || step == null)
            {
                return;
            }

            string text = step.Text ?? string.Empty;
            var preferred = _label.GetPreferredValues(text, 720f, 0f);
            float width = Mathf.Max(defaultSize.x, preferred.x + panelPadding.x * 2f);
            float height = Mathf.Max(defaultSize.y, preferred.y + panelPadding.y * 2f);

            _panelRoot.sizeDelta = new Vector2(width, height);
        }

        private Vector2 ResolveAnchoredPosition(UISequencePlaybackRequest request, UISequenceStep step)
        {
            Vector2 screenPoint = ResolveScreenPoint(request) + step.Offset;
            if (_canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, null, out var localPoint))
            {
                return localPoint;
            }

            return step.Offset;
        }

        private Vector2 ResolveScreenPoint(UISequencePlaybackRequest request)
        {
            Vector2 screenOffset = request != null ? request.ScreenOffset : Vector2.zero;

            if (request != null && request.Anchor != null)
            {
                return RectTransformUtility.WorldToScreenPoint(null, request.Anchor.position) + screenOffset;
            }

            if (request == null || request.UseScreenCenterFallback)
            {
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + screenOffset;
            }

            return screenOffset;
        }

        private int GetValidStepCount(IReadOnlyList<UISequenceStep> steps)
        {
            if (steps == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] != null && !string.IsNullOrWhiteSpace(steps[i].Text))
                {
                    count++;
                }
            }

            return count;
        }
    }
}

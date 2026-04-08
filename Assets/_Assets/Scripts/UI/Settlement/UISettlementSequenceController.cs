using System;
using System.Collections.Generic;
using BaoZuPo.Core;
using BaoZuPo.GameFlow;
using BaoZuPo.Economy;
using BaoZuPo.UI.Common.Animation;
using BaoZuPo.UI.Common.FeedbackPopup;
using Martian.EventBus;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BaoZuPo.UI.Settlement
{
    /// <summary>
    /// 结算演示控制器，管理结算演示序列的队列、执行和金钱转账动画。
    /// 支持并行、顺序和聚合等多种播放方式的阶段执行。
    /// 在结算阶段订阅游戏事件，使用反馈系统播放结算视觉和音效。
    /// 管理金钱转账动画，从源（房间/合约/放置区）飞向顶部金钱显示。
    /// </summary>
    public class UISettlementSequenceController : MonoBehaviour
    {
        [Header("Transfer View")]
        [SerializeField] private RectTransform transferLayerRoot;
        [SerializeField] private CanvasGroup transferLayerGroup;
        [SerializeField] private TextMeshProUGUI transferLabel;
        [SerializeField] private Color transferTextColor = new Color(1f, 0.86f, 0.32f, 1f);
        [SerializeField] private float transferMoveSeconds = 0.25f;
        [SerializeField] private float transferFadeInSeconds = 0.08f;
        [SerializeField] private float transferFadeOutSeconds = 0.08f;
        [SerializeField] private float transferScale = 1.02f;

        [Header("Playback Timing")]
        [SerializeField] private float entryGapSeconds = 0.04f;
        [SerializeField] private float stageGapSeconds = 0.1f;

        private readonly Queue<UISettlementPlaybackBatch> _pendingBatches = new();
        private Sequence _activeTransferSequence;
        private Tween _activePlaybackDelayTween;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private UIFeedbackPopupLayer _popupLayer;
        private bool _isPlaybackRunning;
        private bool _isSettling;
        private bool _runtimeTransferViewBuilt;

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.SettlementSequenceQueued>(OnSettlementQueued);
            EventBus.Subscribe<GameEvents.TurnEnded>(OnTurnEnded);
            EventBus.Subscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.SettlementSequenceQueued>(OnSettlementQueued);
            EventBus.Unsubscribe<GameEvents.TurnEnded>(OnTurnEnded);
            EventBus.Unsubscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            ClearPending();
            HideTransferImmediate();
        }

        private void OnSettlementQueued(GameEvents.SettlementSequenceQueued payload)
        {
            if (payload == null)
            {
                return;
            }

            EnqueueBatch(UISettlementPlaybackBatch.CreateSerial(new[] { payload }));
        }

        private void OnTurnEnded(GameEvents.TurnEnded _)
        {
            FinishPlayback();
        }

        private void OnPhaseChanged(GameEvents.PhaseChanged payload)
        {
            _isSettling = payload.Phase == GamePhase.Settle;
            if (_isSettling)
            {
                if (UIManager.Instance != null && !UIManager.Instance.IsDeferredMoneyDisplayActive)
                {
                    UIManager.Instance.BeginDeferredMoneyDisplay(MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0);
                }

                TryStartPlayback();
                return;
            }

            if (_activeTransferSequence == null && _pendingBatches.Count == 0)
            {
                FinishPlayback();
            }
        }

        public void Queue(GameEvents.SettlementSequenceQueued payload)
        {
            if (payload == null)
            {
                return;
            }

            OnSettlementQueued(payload);
        }

        public void QueueBatch(IReadOnlyList<GameEvents.SettlementSequenceQueued> payloads)
        {
            if (payloads == null)
            {
                return;
            }

            EnqueueBatch(UISettlementPlaybackBatch.CreateSerial(payloads));
        }

        public void Queue(UISettlementPlaybackBatch batch)
        {
            EnqueueBatch(batch);
        }

        private void EnqueueBatch(UISettlementPlaybackBatch batch)
        {
            if (batch == null || (batch.IsEmpty && !ShouldFinalizeBatch(batch)))
            {
                return;
            }

            _pendingBatches.Enqueue(batch);

            if (UIManager.Instance != null && !UIManager.Instance.IsDeferredMoneyDisplayActive)
            {
                int startValue = batch.PlayMoneyJumpOnBatchEnd
                    ? batch.DeferredMoneyStartValue
                    : MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;
                UIManager.Instance.BeginDeferredMoneyDisplay(startValue);
            }

            if (_isSettling)
            {
                TryStartPlayback();
            }
        }

        private void TryStartPlayback()
        {
            if (!_isSettling)
            {
                return;
            }

            if (_isPlaybackRunning)
            {
                return;
            }

            if (_activeTransferSequence != null)
            {
                return;
            }

            if (_pendingBatches.Count == 0)
            {
                return;
            }

            _isPlaybackRunning = true;
            var batch = _pendingBatches.Dequeue();
            PlayBatch(batch, () => OnBatchCompleted(batch));
        }

        private void PlayBatch(UISettlementPlaybackBatch batch, Action onCompleted)
        {
            if (batch == null || batch.Stages == null || batch.Stages.Count == 0)
            {
                onCompleted?.Invoke();
                return;
            }

            PlayStage(batch.Stages, 0, onCompleted);
        }

        private void PlayStage(IReadOnlyList<UISettlementPlaybackStage> stages, int index, Action onCompleted)
        {
            if (stages == null || index >= stages.Count)
            {
                onCompleted?.Invoke();
                return;
            }

            var stage = stages[index];
            if (stage == null || stage.Entries == null || stage.Entries.Count == 0)
            {
                PlayNextStageAfterGap(stages, index, onCompleted);
                return;
            }

            switch (stage.Kind)
            {
                case UISettlementPlaybackStageKind.Parallel:
                    PlayParallel(stage.Entries, () => PlayNextStageAfterGap(stages, index, onCompleted));
                    break;
                case UISettlementPlaybackStageKind.Barrier:
                    PlayNextStageAfterGap(stages, index, onCompleted);
                    break;
                case UISettlementPlaybackStageKind.Aggregate:
                case UISettlementPlaybackStageKind.Serial:
                default:
                    PlaySerial(stage.Entries, 0, () => PlayNextStageAfterGap(stages, index, onCompleted));
                    break;
            }
        }

        private void PlaySerial(IReadOnlyList<UISettlementPlaybackEntry> entries, int index, Action onCompleted)
        {
            if (entries == null || index >= entries.Count)
            {
                onCompleted?.Invoke();
                return;
            }

            var entry = entries[index];
            PlayEntry(entry, () => PlayNextSerialEntry(entries, index, onCompleted));
        }

        private void PlayParallel(IReadOnlyList<UISettlementPlaybackEntry> entries, Action onCompleted)
        {
            if (entries == null || entries.Count == 0)
            {
                onCompleted?.Invoke();
                return;
            }

            int remaining = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                PlayEntry(entries[i], () =>
                {
                    remaining--;
                    if (remaining <= 0)
                    {
                        onCompleted?.Invoke();
                    }
                });
            }
        }

        private void PlayEntry(UISettlementPlaybackEntry entry, Action onCompleted)
        {
            if (entry == null || entry.Payload == null)
            {
                onCompleted?.Invoke();
                return;
            }

            if (!TryPlayPopupSettlementEntry(entry, onCompleted))
            {
                onCompleted?.Invoke();
            }
        }

        private bool TryPlayPopupSettlementEntry(UISettlementPlaybackEntry entry, Action onCompleted)
        {
            var payload = entry != null ? entry.Payload : null;
            if (payload == null || payload.Steps == null || payload.Steps.Length == 0)
            {
                return false;
            }

            var layer = ResolvePopupLayer();
            if (layer == null)
            {
                return false;
            }

            PlayPopupSettlementStep(layer, payload, 0, onCompleted);
            return true;
        }

        private void PlayPopupSettlementStep(
            UIFeedbackPopupLayer layer,
            GameEvents.SettlementSequenceQueued payload,
            int stepIndex,
            Action onCompleted)
        {
            if (payload == null || payload.Steps == null || stepIndex >= payload.Steps.Length)
            {
                onCompleted?.Invoke();
                return;
            }

            var step = payload.Steps[stepIndex];
            string text = FormatPopupStepText(payload, step);
            if (string.IsNullOrWhiteSpace(text))
            {
                PlayPopupSettlementStep(layer, payload, stepIndex + 1, onCompleted);
                return;
            }

            bool isFinalStep = step.Kind == GameEvents.SettlementStepKind.Final;
            layer.Show(new UIFeedbackPopupRequest
            {
                Anchor = ResolveSourceAnchor(payload),
                Text = text,
                Category = ResolvePopupCategory(step, isFinalStep),
                IsFinal = isFinalStep,
                UseScreenCenterFallback = true,
                ScreenOffset = ResolveSourceOffset(payload),
                AnchorFeedback = PlayPopupAnchorFeedback,
                Completed = () => PlayPopupSettlementStep(layer, payload, stepIndex + 1, onCompleted)
            });
        }

        private static void PlayPopupAnchorFeedback(RectTransform anchor)
        {
            UIAnimationTweenUtility.PunchScalePreserveBase(anchor, 0.045f, 0.14f, 6, 0.45f);
        }

        private void PlayNextSerialEntry(IReadOnlyList<UISettlementPlaybackEntry> entries, int currentIndex, Action onCompleted)
        {
            int nextIndex = currentIndex + 1;
            if (entries == null || nextIndex >= entries.Count || entryGapSeconds <= 0f)
            {
                PlaySerial(entries, nextIndex, onCompleted);
                return;
            }

            SchedulePlaybackDelay(entryGapSeconds, () => PlaySerial(entries, nextIndex, onCompleted));
        }

        private void PlayNextStageAfterGap(IReadOnlyList<UISettlementPlaybackStage> stages, int currentIndex, Action onCompleted)
        {
            int nextIndex = currentIndex + 1;
            if (stages == null || nextIndex >= stages.Count || stageGapSeconds <= 0f)
            {
                PlayStage(stages, nextIndex, onCompleted);
                return;
            }

            SchedulePlaybackDelay(stageGapSeconds, () => PlayStage(stages, nextIndex, onCompleted));
        }

        private void SchedulePlaybackDelay(float seconds, Action callback)
        {
            _activePlaybackDelayTween?.Kill(false);
            _activePlaybackDelayTween = DOVirtual.DelayedCall(seconds, () =>
            {
                _activePlaybackDelayTween = null;
                callback?.Invoke();
            }).SetUpdate(true);
        }

        private void PlayTransfer(UISettlementPlaybackEntry entry, Action onCompleted)
        {
            EnsureTransferView();

            if (entry == null || entry.Payload == null || UIManager.Instance == null)
            {
                onCompleted?.Invoke();
                return;
            }

            if (entry.Payload.FinalAmount == 0)
            {
                CompleteEntry(entry, onCompleted);
                return;
            }

            if (transferLayerRoot == null || transferLayerGroup == null)
            {
                UIManager.Instance.CommitDisplayedDelta(entry.Payload.FinalAmount);
                CompleteEntry(entry, onCompleted);
                return;
            }

            var topBarTarget = UIManager.Instance.ResolveMoneyTargetAnchor();
            if (topBarTarget == null)
            {
                UIManager.Instance.CommitDisplayedDelta(entry.Payload.FinalAmount);
                CompleteEntry(entry, onCompleted);
                return;
            }

            var sourceAnchor = ResolveSourceAnchor(entry.Payload);
            Vector2 sourcePoint = ResolveScreenPoint(sourceAnchor, ResolveSourceOffset(entry.Payload));
            Vector2 targetPoint = ResolveScreenPoint(topBarTarget, Vector2.zero);

            string transferText = FormatSignedAmount(entry.Payload.FinalAmount);
            _activeTransferSequence?.Kill(false);
            _activeTransferSequence = DOTween.Sequence().SetUpdate(true);

            _activeTransferSequence.AppendCallback(() =>
            {
                if (transferLayerRoot != null)
                {
                    transferLayerRoot.gameObject.SetActive(true);
                    transferLayerRoot.anchoredPosition = sourcePoint;
                    transferLayerRoot.localScale = Vector3.one * 0.94f;
                }

                if (transferLayerGroup != null)
                {
                    transferLayerGroup.alpha = 0f;
                }

                if (transferLabel != null)
                {
                    transferLabel.text = transferText;
                    transferLabel.color = transferTextColor;
                }
            });

            if (transferFadeInSeconds > 0f && transferLayerGroup != null)
            {
                _activeTransferSequence.Append(transferLayerGroup.DOFade(1f, transferFadeInSeconds).SetEase(Ease.OutQuad));
                _activeTransferSequence.Join(transferLayerRoot.DOScale(transferScale, transferFadeInSeconds).SetEase(Ease.OutQuad));
            }
            else
            {
                _activeTransferSequence.AppendCallback(() =>
                {
                    if (transferLayerGroup != null)
                    {
                        transferLayerGroup.alpha = 1f;
                    }

                    if (transferLayerRoot != null)
                    {
                        transferLayerRoot.localScale = Vector3.one * transferScale;
                    }
                });
            }

            if (transferLayerRoot != null)
            {
                _activeTransferSequence.Join(transferLayerRoot.DOAnchorPos(targetPoint, Mathf.Max(0.01f, transferMoveSeconds)).SetEase(Ease.OutCubic));
            }

            _activeTransferSequence.AppendCallback(() =>
            {
                UIManager.Instance.CommitDisplayedDelta(entry.Payload.FinalAmount);
            });

            if (transferFadeOutSeconds > 0f && transferLayerGroup != null)
            {
                _activeTransferSequence.Append(transferLayerGroup.DOFade(0f, transferFadeOutSeconds).SetEase(Ease.InQuad));
            }

            _activeTransferSequence.OnComplete(() =>
            {
                HideTransferImmediate();
                CompleteEntry(entry, onCompleted);
            });

            _activeTransferSequence.Play();
        }

        private static void CompleteEntry(UISettlementPlaybackEntry entry, Action onCompleted)
        {
            onCompleted?.Invoke();
        }

        private void OnBatchCompleted(UISettlementPlaybackBatch batch)
        {
            PlayBatchMoneyJump(batch, () =>
            {
                if (batch != null && batch.PublishCompletionOnBatchEnd && !string.IsNullOrWhiteSpace(batch.CompletionBatchId))
                {
                    EventBus.Publish(new GameEvents.SettlementPlaybackCompleted
                    {
                        BatchId = batch.CompletionBatchId
                    });
                }

                _isPlaybackRunning = false;
                if (_pendingBatches.Count > 0)
                {
                    TryStartPlayback();
                    return;
                }

                FinishPlayback();
            });
        }

        private void FinishPlayback()
        {
            if (_isPlaybackRunning)
            {
                return;
            }

            if (_pendingBatches.Count > 0 || _activeTransferSequence != null || _activePlaybackDelayTween != null)
            {
                return;
            }

            UIManager.Instance?.EndDeferredMoneyDisplay();
            UIManager.Instance?.RefreshAll();
        }

        private void ClearPending()
        {
            _pendingBatches.Clear();
            _isPlaybackRunning = false;
            _isSettling = false;
            _activeTransferSequence?.Kill(false);
            _activeTransferSequence = null;
            _activePlaybackDelayTween?.Kill(false);
            _activePlaybackDelayTween = null;
        }

        private void PlayBatchMoneyJump(UISettlementPlaybackBatch batch, Action onCompleted)
        {
            if (!ShouldFinalizeBatch(batch))
            {
                onCompleted?.Invoke();
                return;
            }

            int totalDelta = batch.TotalDelta;
            if (totalDelta == 0 || UIManager.Instance == null)
            {
                onCompleted?.Invoke();
                return;
            }

            UIManager.Instance.CommitDisplayedDelta(totalDelta);
            if (!TryPlayPopupBatchMoneyJump(totalDelta, onCompleted))
            {
                onCompleted?.Invoke();
            }
        }

        private bool TryPlayPopupBatchMoneyJump(int totalDelta, Action onCompleted)
        {
            var layer = ResolvePopupLayer();
            if (layer == null || UIManager.Instance == null)
            {
                return false;
            }

            RectTransform anchor = UIManager.Instance.ResolveMoneyTargetAnchor();
            float verticalGap = UIManager.Instance.topBar != null
                ? UIManager.Instance.topBar.SettlementTotalPopupVerticalGap
                : 40f;

            layer.Show(new UIFeedbackPopupRequest
            {
                Anchor = anchor,
                Text = FormatSignedAmount(totalDelta),
                Category = totalDelta < 0 ? UIFeedbackPopupCategory.Negative : UIFeedbackPopupCategory.Final,
                IsFinal = totalDelta >= 0,
                UseScreenCenterFallback = anchor == null,
                ScreenOffset = ResolveAboveAnchorOffset(anchor, verticalGap),
                AnchorFeedback = PlayPopupAnchorFeedback,
                Completed = onCompleted
            });
            return true;
        }

        private static bool ShouldFinalizeBatch(UISettlementPlaybackBatch batch)
        {
            return batch != null && batch.PlayMoneyJumpOnBatchEnd;
        }

        private void EnsureTransferView()
        {
            if (_runtimeTransferViewBuilt && transferLayerRoot != null && transferLayerGroup != null && transferLabel != null)
            {
                return;
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_canvas == null && UIManager.Instance != null)
            {
                _canvas = UIManager.Instance.GetComponentInParent<Canvas>();
            }

            if (_canvas != null)
            {
                _canvasRect = _canvas.transform as RectTransform;
            }

            if (transferLayerRoot == null)
            {
                transferLayerRoot = transform as RectTransform;
                if (transferLayerRoot == null)
                {
                    return;
                }
            }

            if (transferLayerGroup == null)
            {
                transferLayerGroup = transferLayerRoot.GetComponent<CanvasGroup>();
            }

            if (transferLayerGroup == null)
            {
                return;
            }

            if (transferLabel == null)
            {
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(transferLayerRoot, false);

                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(20f, 10f);
                labelRect.offsetMax = new Vector2(-20f, -10f);

                transferLabel = labelObject.GetComponent<TextMeshProUGUI>();
                transferLabel.fontSize = 24f;
                transferLabel.alignment = TextAlignmentOptions.Center;
                transferLabel.color = transferTextColor;
                transferLabel.raycastTarget = false;
            }

            transferLayerGroup.blocksRaycasts = false;
            transferLayerGroup.interactable = false;
            transferLayerRoot.gameObject.SetActive(false);
            _runtimeTransferViewBuilt = true;
        }

        private void HideTransferImmediate()
        {
            _activeTransferSequence?.Kill(false);
            _activeTransferSequence = null;

            if (transferLayerRoot != null)
            {
                transferLayerRoot.gameObject.SetActive(false);
                transferLayerRoot.localScale = Vector3.one;
            }

            if (transferLayerGroup != null)
            {
                transferLayerGroup.alpha = 0f;
            }
        }

        private RectTransform ResolveSourceAnchor(GameEvents.SettlementSequenceQueued payload)
        {
            if (payload == null || UIManager.Instance == null || UIManager.Instance.boardPanel == null)
            {
                return null;
            }

            switch (payload.SourceKind)
            {
                case GameEvents.SettlementSourceKind.Room when payload.Room != null:
                    return UIManager.Instance.boardPanel.ResolveRoomAnchor(payload.Room);
                case GameEvents.SettlementSourceKind.Contract when payload.Card != null:
                    return UIManager.Instance.boardPanel.ResolveContractAnchor(payload.Card);
                default:
                    return UIManager.Instance.boardPanel.ResolvePlayAreaAnchor();
            }
        }

        private static Vector2 ResolveSourceOffset(GameEvents.SettlementSequenceQueued payload)
        {
            return payload != null && payload.SourceKind == GameEvents.SettlementSourceKind.Room
                ? new Vector2(0f, 140f)
                : payload != null && payload.SourceKind == GameEvents.SettlementSourceKind.Contract
                    ? new Vector2(0f, 120f)
                    : new Vector2(0f, 48f);
        }

        private Vector2 ResolveScreenPoint(RectTransform anchor, Vector2 screenOffset)
        {
            if (_canvasRect == null && _canvas != null)
            {
                _canvasRect = _canvas.transform as RectTransform;
            }

            if (anchor != null)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, anchor.position) + screenOffset;
                if (_canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, null, out var localPoint))
                {
                    return localPoint;
                }

                return screenPoint;
            }

            Vector2 fallbackScreenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + screenOffset;
            if (_canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, fallbackScreenPoint, null, out var fallbackLocalPoint))
            {
                return fallbackLocalPoint;
            }

            return fallbackScreenPoint;
        }

        private static Vector2 ResolveAboveAnchorOffset(RectTransform anchor, float verticalGap)
        {
            if (anchor == null)
            {
                return new Vector2(0f, 54f);
            }

            var rect = anchor.rect;
            return new Vector2(
                rect.width * (0.5f - anchor.pivot.x),
                rect.height * (1f - anchor.pivot.y) + verticalGap);
        }

        private UIFeedbackPopupLayer ResolvePopupLayer()
        {
            if (_popupLayer != null)
            {
                return _popupLayer;
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_canvas == null && UIManager.Instance != null)
            {
                _canvas = UIManager.Instance.GetComponentInParent<Canvas>();
            }

            _popupLayer = UIFeedbackPopupLayer.GetOrCreate(_canvas);
            return _popupLayer;
        }

        private static string FormatPopupStepText(GameEvents.SettlementSequenceQueued payload, GameEvents.SettlementStep step)
        {
            string label = ResolvePopupStepLabel(payload, step);
            if (!string.IsNullOrWhiteSpace(label))
            {
                if (step.IsMultiplier)
                {
                    float multiplier = step.Amount / 100f;
                    return $"{label} x{multiplier:0.##}";
                }

                if (label.Contains("{0}"))
                {
                    return string.Format(label, FormatSignedAmount(step.Amount));
                }

                return $"{label} {FormatSignedAmount(step.Amount)}";
            }

            if (step.IsMultiplier)
            {
                float multiplier = step.Amount / 100f;
                return $"x{multiplier:0.##}";
            }

            return FormatSignedAmount(step.Amount);
        }

        private static string ResolvePopupStepLabel(GameEvents.SettlementSequenceQueued payload, GameEvents.SettlementStep step)
        {
            if (!string.IsNullOrWhiteSpace(step.Label))
            {
                return step.Label;
            }

            if (payload != null && payload.Card != null && payload.Card.Data != null && step.Kind == GameEvents.SettlementStepKind.Delta)
            {
                return CardText.Name(payload.Card);
            }

            return step.Kind switch
            {
                GameEvents.SettlementStepKind.Base => GameText.SettlementBase,
                GameEvents.SettlementStepKind.Delta => GameText.SettlementBonus,
                GameEvents.SettlementStepKind.Multiplier => GameText.SettlementMultiplier,
                GameEvents.SettlementStepKind.Final => GameText.SettlementFinal,
                _ => null
            };
        }

        private static string ResolvePopupCategory(GameEvents.SettlementStep step, bool isFinalStep)
        {
            if (isFinalStep || step.Kind == GameEvents.SettlementStepKind.Final)
            {
                return UIFeedbackPopupCategory.Final;
            }

            if (step.IsMultiplier || step.Kind == GameEvents.SettlementStepKind.Multiplier)
            {
                return UIFeedbackPopupCategory.Multiplier;
            }

            return step.Amount < 0 ? UIFeedbackPopupCategory.Negative : UIFeedbackPopupCategory.Positive;
        }

        private static string FormatSignedAmount(int amount)
        {
            string sign = amount > 0 ? "+" : string.Empty;
            return $"{sign}{amount}";
        }
    }
}

using System;
using System.Collections.Generic;
using BaoZuPo.Board;
using BaoZuPo.Core;
using BaoZuPo.GameFlow;
using BaoZuPo.Economy;
using BaoZuPo.UI.Common.Animation;
using BaoZuPo.Integration.Feel;
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

        [Header("Accumulator View")]
        [SerializeField] private RectTransform accumulatorRoot;
        [SerializeField] private CanvasGroup accumulatorGroup;
        [SerializeField] private TextMeshProUGUI accumulatorLabel;
        [SerializeField] private Color accumulatorTextColor = new Color(1f, 0.86f, 0.32f, 1f);
        [SerializeField] private float accumulatorCountDuration = 0.3f;
        [SerializeField] private float accumulatorFlyDuration = 0.4f;
        [SerializeField] private float accumulatorPauseBeforeFly = 0.4f;

        [Header("Room Accumulator View")]
        [SerializeField] private RectTransform roomAccumulatorRoot;
        [SerializeField] private CanvasGroup roomAccumulatorGroup;
        [SerializeField] private TextMeshProUGUI roomAccumulatorLabel;
        [SerializeField] private Color roomAccumulatorTextColor = new Color(1f, 0.92f, 0.62f, 1f);
        [SerializeField] private float roomAccumulatorCountDuration = 0.18f;
        [SerializeField] private float roomAccumulatorFlyDuration = 0.28f;
        [SerializeField] private float roomAccumulatorPauseBeforeFly = 0.3f;

        [Header("Playback Timing")]
        [SerializeField] private float entryGapSeconds = 0.04f;
        [SerializeField] private float stageGapSeconds = 0.1f;
        [SerializeField] private float stepGapSeconds = 0.12f;

        [Header("Source Feedback")]
        [SerializeField] private float sourceTriggerPunchScale = 0.055f;
        [SerializeField] private float sourceTriggerPunchDuration = 0.15f;
        [SerializeField] private int sourceTriggerPunchVibrato = 6;
        [SerializeField] private float sourceTriggerPunchElasticity = 0.45f;

        private readonly Queue<UISettlementPlaybackBatch> _pendingBatches = new();
        private readonly List<string> _pendingCompletionBatchIds = new();
        private Sequence _activeTransferSequence;
        private Tween _activePlaybackDelayTween;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private UIFeedbackPopupLayer _popupLayer;
        private bool _isPlaybackRunning;
        private bool _isSettling;
        private bool _transferViewReady;
        private int _accumulatedAmount;
        private float _accumulatorDisplayFloat;
        private Tween _accumulatorCountTween;
        private Sequence _accumulatorFlySequence;
        private Action _onAccumulatorCountComplete;
        private bool _isFinishingPlayback;
        private Vector2 _accumulatorHomePosition;
        private int _roomAccumulatedAmount;
        private float _roomAccumulatedFloat;
        private int _roomEntryBaseAmount;
        private int _roomEntryAccumulatedAmount;
        private Tween _roomAccumulatorCountTween;
        private Sequence _roomAccumulatorFlySequence;

        private void Start()
        {
            if (accumulatorRoot != null)
                _accumulatorHomePosition = accumulatorRoot.anchoredPosition;
        }

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
                ResetAccumulator();
                ShowAccumulatorInitial();
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

            if (IsRoomSettlementStage(stage))
            {
                PlayRoomStage(stage.Entries, () => PlayNextStageAfterGap(stages, index, onCompleted));
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

        private static bool IsRoomSettlementStage(UISettlementPlaybackStage stage)
        {
            if (stage == null
                || (stage.Kind != UISettlementPlaybackStageKind.Serial && stage.Kind != UISettlementPlaybackStageKind.Aggregate)
                || stage.Entries == null
                || stage.Entries.Count == 0)
            {
                return false;
            }

            RoomSlot room = null;
            for (int i = 0; i < stage.Entries.Count; i++)
            {
                var payload = stage.Entries[i]?.Payload;
                if (payload == null || payload.SourceKind != GameEvents.SettlementSourceKind.Room || payload.Room == null)
                {
                    return false;
                }

                if (room == null)
                {
                    room = payload.Room;
                    continue;
                }

                if (!ReferenceEquals(room, payload.Room))
                {
                    return false;
                }
            }

            return room != null;
        }

        private void PlayRoomStage(IReadOnlyList<UISettlementPlaybackEntry> entries, Action onCompleted)
        {
            var firstPayload = FindFirstPayload(entries);
            if (firstPayload == null)
            {
                onCompleted?.Invoke();
                return;
            }

            _roomAccumulatedAmount = 0;
            _roomAccumulatedFloat = 0f;
            _roomEntryBaseAmount = 0;
            _roomEntryAccumulatedAmount = 0;
            ShowRoomAccumulatorForEntry(firstPayload);
            PlayRoomStageEntry(entries, 0, () => FinalizeRoomStage(entries, onCompleted));
        }

        private void PlayRoomStageEntry(IReadOnlyList<UISettlementPlaybackEntry> entries, int index, Action onCompleted)
        {
            if (entries == null || index >= entries.Count)
            {
                onCompleted?.Invoke();
                return;
            }

            var entry = entries[index];
            if (entry == null || entry.Payload == null)
            {
                PlayNextRoomStageEntry(entries, index, onCompleted);
                return;
            }

            _roomEntryBaseAmount = _roomAccumulatedAmount;
            _roomEntryAccumulatedAmount = 0;

            void OnPopupCompleted()
            {
                _roomAccumulatedAmount = _roomEntryBaseAmount + entry.FinalAmount;
                PlayNextRoomStageEntry(entries, index, onCompleted);
            }

            if (!TryPlayPopupSettlementEntry(entry, OnPopupCompleted))
            {
                OnPopupCompleted();
            }
        }

        private void PlayNextRoomStageEntry(IReadOnlyList<UISettlementPlaybackEntry> entries, int currentIndex, Action onCompleted)
        {
            int nextIndex = currentIndex + 1;
            if (entries == null || nextIndex >= entries.Count || entryGapSeconds <= 0f)
            {
                PlayRoomStageEntry(entries, nextIndex, onCompleted);
                return;
            }

            SchedulePlaybackDelay(entryGapSeconds, () => PlayRoomStageEntry(entries, nextIndex, onCompleted));
        }

        private void FinalizeRoomStage(IReadOnlyList<UISettlementPlaybackEntry> entries, Action onCompleted)
        {
            int finalAmount = SumFinalAmount(entries);
            if (finalAmount != 0 && _roomAccumulatedAmount != finalAmount)
            {
                int from = Mathf.RoundToInt(_roomAccumulatedFloat);
                _roomAccumulatedAmount = finalAmount;
                TweenRoomAccumulatorLabel(from, finalAmount);
            }

            if (finalAmount != 0 && roomAccumulatorRoot != null && roomAccumulatorRoot.gameObject.activeSelf)
            {
                SchedulePlaybackDelay(roomAccumulatorPauseBeforeFly, () =>
                    FlyRoomAccumulatorToTotal(() =>
                    {
                        AccumulateToTotal(finalAmount);
                        onCompleted?.Invoke();
                    }));
            }
            else
            {
                HideRoomAccumulatorImmediate();
                if (finalAmount != 0) AccumulateToTotal(finalAmount);
                onCompleted?.Invoke();
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

            _roomAccumulatedAmount = 0;
            _roomAccumulatedFloat = 0f;
            _roomEntryBaseAmount = 0;
            _roomEntryAccumulatedAmount = 0;
            ShowRoomAccumulatorForEntry(entry.Payload);

            void OnPopupCompleted() => FinalizeRoomEntry(entry, onCompleted);

            if (!TryPlayPopupSettlementEntry(entry, OnPopupCompleted))
                FinalizeRoomEntry(entry, onCompleted);
        }

        private void FinalizeRoomEntry(UISettlementPlaybackEntry entry, Action onCompleted)
        {
            int finalAmount = entry?.FinalAmount ?? 0;
            if (finalAmount != 0 && _roomAccumulatedAmount != finalAmount)
            {
                int from = Mathf.RoundToInt(_roomAccumulatedFloat);
                _roomAccumulatedAmount = finalAmount;
                TweenRoomAccumulatorLabel(from, finalAmount);
            }

            if (finalAmount != 0 && roomAccumulatorRoot != null && roomAccumulatorRoot.gameObject.activeSelf)
            {
                SchedulePlaybackDelay(roomAccumulatorPauseBeforeFly, () =>
                    FlyRoomAccumulatorToTotal(() =>
                    {
                        AccumulateToTotal(finalAmount);
                        onCompleted?.Invoke();
                    }));
            }
            else
            {
                HideRoomAccumulatorImmediate();
                if (finalAmount != 0) AccumulateToTotal(finalAmount);
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

            UpdateRoomAccumulatorForStep(step);
            BaoZuPoFeelFeedbackInstaller.Active?.FeelBackend?.PlaySlot(FeelFeedbackSlots.SettlementStep);

            bool isFinalStep = step.Kind == GameEvents.SettlementStepKind.Final;
            var sourceAnchor = ResolveSourceAnchor(payload, step);
            layer.Show(new UIFeedbackPopupRequest
            {
                Anchor = sourceAnchor,
                Text = text,
                Category = ResolvePopupCategory(step, isFinalStep),
                IsFinal = isFinalStep,
                UseScreenCenterFallback = true,
                ScreenOffset = ResolveSourceOffset(payload, sourceAnchor),
                AnchorFeedback = PlaySourceTriggeredFeedback,
                Completed = () =>
                {
                    if (stepGapSeconds > 0f)
                        SchedulePlaybackDelay(stepGapSeconds, () => PlayPopupSettlementStep(layer, payload, stepIndex + 1, onCompleted));
                    else
                        PlayPopupSettlementStep(layer, payload, stepIndex + 1, onCompleted);
                }
            });
        }

        private void PlaySourceTriggeredFeedback(RectTransform anchor)
        {
            if (anchor == null)
            {
                return;
            }

            float feelDuration = BaoZuPoFeelFeedbackInstaller.Active?.FeelBackend?.PlaySlotAttached(
                FeelFeedbackSlots.SourceTriggered,
                anchor,
                "SettlementSourceTriggered") ?? 0f;

            if (feelDuration > 0f)
            {
                return;
            }

            UIAnimationTweenUtility.PunchScalePreserveBase(
                anchor,
                sourceTriggerPunchScale,
                sourceTriggerPunchDuration,
                sourceTriggerPunchVibrato,
                sourceTriggerPunchElasticity);
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
            if (!EnsureTransferView())
            {
                onCompleted?.Invoke();
                return;
            }

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
            Vector2 sourcePoint = ResolveScreenPoint(sourceAnchor, ResolveSourceOffset(entry.Payload, sourceAnchor));
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
                    _pendingCompletionBatchIds.Add(batch.CompletionBatchId);
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

            if (_isFinishingPlayback)
            {
                return;
            }

            if (_accumulatedAmount != 0 && accumulatorRoot != null && accumulatorRoot.gameObject.activeSelf)
            {
                _isFinishingPlayback = true;
                void DoFly() => SchedulePlaybackDelay(accumulatorPauseBeforeFly, () => FlyAccumulatorToTopBar(() =>
                {
                    _isFinishingPlayback = false;
                    UIManager.Instance?.RefreshAll();
                    UIManager.Instance?.EndDeferredMoneyDisplay(PublishPendingCompletions);
                }));
                if (_accumulatorCountTween != null)
                    _onAccumulatorCountComplete = DoFly;
                else
                    DoFly();
            }
            else
            {
                UIManager.Instance?.RefreshAll();
                UIManager.Instance?.EndDeferredMoneyDisplay(PublishPendingCompletions);
            }
        }

        private void ClearPending()
        {
            _pendingBatches.Clear();
            _pendingCompletionBatchIds.Clear();
            _isPlaybackRunning = false;
            _isSettling = false;
            _isFinishingPlayback = false;
            _activeTransferSequence?.Kill(false);
            _activeTransferSequence = null;
            _activePlaybackDelayTween?.Kill(false);
            _activePlaybackDelayTween = null;
            HideRoomAccumulatorImmediate();
            ResetAccumulator();
        }

        private void PlayBatchMoneyJump(UISettlementPlaybackBatch batch, Action onCompleted)
        {
            onCompleted?.Invoke();
        }

        private static bool ShouldFinalizeBatch(UISettlementPlaybackBatch batch)
        {
            return batch != null && batch.PlayMoneyJumpOnBatchEnd;
        }

        private bool EnsureTransferView()
        {
            if (_transferViewReady) return true;

            EnsureCanvas();
            if (_canvas != null)
                _canvasRect = _canvas.transform as RectTransform;

            if (transferLayerRoot == null || transferLayerGroup == null || transferLabel == null)
            {
                Debug.LogWarning("[UISettlementSequenceController] Transfer view references are not assigned. Transfer animation will be skipped.");
                return false;
            }

            transferLayerGroup.blocksRaycasts = false;
            transferLayerGroup.interactable = false;
            transferLayerRoot.gameObject.SetActive(false);
            _transferViewReady = true;
            return true;
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

        private RectTransform ResolveSourceAnchor(GameEvents.SettlementSequenceQueued payload, GameEvents.SettlementStep step)
        {
            if (payload == null || UIManager.Instance == null || UIManager.Instance.boardPanel == null)
            {
                return null;
            }

            if (payload.SourceKind == GameEvents.SettlementSourceKind.Room && step.SourceCard != null)
            {
                return UIManager.Instance.boardPanel.ResolveRoomCardAnchor(payload.Room, step.SourceCard);
            }

            return ResolveSourceAnchor(payload);
        }

        private static Vector2 ResolveSourceOffset(GameEvents.SettlementSequenceQueued payload, RectTransform anchor)
        {
            if (payload != null && payload.SourceKind == GameEvents.SettlementSourceKind.Contract)
            {
                return ResolveAboveAnchorOffset(anchor, 18f);
            }

            return payload != null && payload.SourceKind == GameEvents.SettlementSourceKind.Room
                ? new Vector2(0f, 140f)
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

        private void EnsureCanvas()
        {
            if (_canvas != null) return;
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null && UIManager.Instance != null)
                _canvas = UIManager.Instance.GetComponentInParent<Canvas>();
            if (_canvas != null)
                _canvasRect = _canvas.transform as RectTransform;
        }

        private UIFeedbackPopupLayer ResolvePopupLayer()
        {
            if (_popupLayer != null)
            {
                return _popupLayer;
            }

            EnsureCanvas();
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

        private void AccumulateToTotal(int amount)
        {
            if (amount == 0 || accumulatorRoot == null || accumulatorLabel == null)
            {
                return;
            }
            ShowAccumulator();
            int from = Mathf.RoundToInt(_accumulatorDisplayFloat);
            _accumulatedAmount += amount;
            TweenAccumulatorLabel(from, _accumulatedAmount);
            UIAnimationTweenUtility.PunchScalePreserveBase(accumulatorRoot, 0.07f, 0.22f, 6, 0.55f);
        }

        private void ShowRoomAccumulatorForEntry(GameEvents.SettlementSequenceQueued payload)
        {
            if (roomAccumulatorRoot == null || roomAccumulatorLabel == null)
            {
                return;
            }
            EnsureCanvas();
            var anchor = ResolveSourceAnchor(payload);
            roomAccumulatorRoot.anchoredPosition = ResolveScreenPoint(anchor, new Vector2(0f, 260f));
            roomAccumulatorRoot.localScale = Vector3.one;
            roomAccumulatorLabel.text = "+0";
            roomAccumulatorLabel.color = roomAccumulatorTextColor;
            _roomAccumulatedFloat = 0f;
            roomAccumulatorRoot.gameObject.SetActive(true);
            if (roomAccumulatorGroup != null)
            {
                roomAccumulatorGroup.alpha = 0f;
                roomAccumulatorGroup.DOFade(1f, 0.18f).SetEase(Ease.OutQuad).SetUpdate(true);
            }
        }

        private void HideRoomAccumulatorImmediate()
        {
            _roomAccumulatorFlySequence?.Kill(false);
            _roomAccumulatorFlySequence = null;
            _roomAccumulatorCountTween?.Kill(false);
            _roomAccumulatorCountTween = null;
            if (roomAccumulatorRoot != null)
            {
                roomAccumulatorRoot.gameObject.SetActive(false);
            }
            if (roomAccumulatorGroup != null)
            {
                roomAccumulatorGroup.alpha = 0f;
            }
        }

        private void UpdateRoomAccumulatorForStep(GameEvents.SettlementStep step)
        {
            if (roomAccumulatorRoot == null || roomAccumulatorLabel == null || !roomAccumulatorRoot.gameObject.activeSelf)
            {
                return;
            }
            int from = Mathf.RoundToInt(_roomAccumulatedFloat);
            if (step.IsMultiplier)
                _roomEntryAccumulatedAmount = Mathf.RoundToInt(_roomEntryAccumulatedAmount * (step.Amount / 100f));
            else if (step.Kind == GameEvents.SettlementStepKind.Final)
                _roomEntryAccumulatedAmount = step.Amount;
            else
                _roomEntryAccumulatedAmount += step.Amount;
            _roomAccumulatedAmount = _roomEntryBaseAmount + _roomEntryAccumulatedAmount;
            TweenRoomAccumulatorLabel(from, _roomAccumulatedAmount);
        }

        private static GameEvents.SettlementSequenceQueued FindFirstPayload(IReadOnlyList<UISettlementPlaybackEntry> entries)
        {
            if (entries == null)
            {
                return null;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i]?.Payload != null)
                {
                    return entries[i].Payload;
                }
            }

            return null;
        }

        private static int SumFinalAmount(IReadOnlyList<UISettlementPlaybackEntry> entries)
        {
            int sum = 0;
            if (entries == null)
            {
                return sum;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                sum += entries[i]?.FinalAmount ?? 0;
            }

            return sum;
        }

        private void TweenRoomAccumulatorLabel(int from, int to)
        {
            if (roomAccumulatorLabel == null)
            {
                return;
            }
            _roomAccumulatorCountTween?.Kill(false);
            _roomAccumulatedFloat = from;
            _roomAccumulatorCountTween = DOTween.To(
                () => _roomAccumulatedFloat,
                x =>
                {
                    _roomAccumulatedFloat = x;
                    roomAccumulatorLabel.text = FormatSignedAmount(Mathf.RoundToInt(x));
                    roomAccumulatorLabel.color = roomAccumulatorTextColor;
                },
                (float)to,
                roomAccumulatorCountDuration
            ).SetEase(Ease.OutQuart).SetUpdate(true).OnComplete(() => _roomAccumulatorCountTween = null);
        }

        private void FlyRoomAccumulatorToTotal(Action onCompleted)
        {
            _roomAccumulatorCountTween?.Kill(false);
            _roomAccumulatorCountTween = null;

            if (roomAccumulatorRoot == null || roomAccumulatorGroup == null)
            {
                HideRoomAccumulatorImmediate();
                onCompleted?.Invoke();
                return;
            }

            if (accumulatorRoot == null || !accumulatorRoot.gameObject.activeSelf)
            {
                HideRoomAccumulatorImmediate();
                onCompleted?.Invoke();
                return;
            }

            if (roomAccumulatorLabel != null)
            {
                roomAccumulatorLabel.text = FormatSignedAmount(_roomAccumulatedAmount);
            }

            Vector2 targetPos = ResolveScreenPoint(accumulatorRoot, Vector2.zero);

            _roomAccumulatorFlySequence?.Kill(false);
            _roomAccumulatorFlySequence = DOTween.Sequence().SetUpdate(true);
            _roomAccumulatorFlySequence.Append(roomAccumulatorRoot.DOAnchorPos(targetPos, roomAccumulatorFlyDuration).SetEase(Ease.InCubic));
            _roomAccumulatorFlySequence.Join(
                roomAccumulatorGroup.DOFade(0f, roomAccumulatorFlyDuration * 0.5f)
                    .SetEase(Ease.InQuad)
                    .SetDelay(roomAccumulatorFlyDuration * 0.5f));
            _roomAccumulatorFlySequence.OnComplete(() =>
            {
                HideRoomAccumulatorImmediate();
                onCompleted?.Invoke();
            });
        }

        private void ShowAccumulatorInitial()
        {
            if (accumulatorRoot == null || accumulatorLabel == null)
            {
                return;
            }
            accumulatorLabel.text = "+0";
            _accumulatorDisplayFloat = 0f;
            accumulatorRoot.anchoredPosition = _accumulatorHomePosition;
            accumulatorRoot.localScale = Vector3.one;
            accumulatorRoot.gameObject.SetActive(true);
            if (accumulatorGroup != null)
            {
                accumulatorGroup.alpha = 0f;
                accumulatorGroup.DOFade(1f, 0.25f).SetEase(Ease.OutQuad).SetUpdate(true);
            }
        }

        private void ShowAccumulator()
        {
            if (accumulatorRoot == null || accumulatorRoot.gameObject.activeSelf)
            {
                return;
            }
            accumulatorRoot.anchoredPosition = _accumulatorHomePosition;
            accumulatorRoot.localScale = Vector3.one;
            accumulatorRoot.gameObject.SetActive(true);
            if (accumulatorGroup != null)
            {
                accumulatorGroup.alpha = 1f;
            }
        }

        private void HideAccumulatorImmediate()
        {
            _accumulatorFlySequence?.Kill(false);
            _accumulatorFlySequence = null;
            _accumulatorCountTween?.Kill(false);
            _accumulatorCountTween = null;
            _onAccumulatorCountComplete = null;
            if (accumulatorRoot != null)
            {
                accumulatorRoot.gameObject.SetActive(false);
            }
            if (accumulatorGroup != null)
            {
                accumulatorGroup.alpha = 0f;
            }
        }

        private void ResetAccumulator()
        {
            HideAccumulatorImmediate();
            _accumulatedAmount = 0;
            _accumulatorDisplayFloat = 0f;
            if (accumulatorLabel != null)
            {
                accumulatorLabel.text = FormatSignedAmount(0);
            }
        }

        private void TweenAccumulatorLabel(int from, int to)
        {
            if (accumulatorLabel == null)
            {
                return;
            }
            _accumulatorCountTween?.Kill(false);
            _accumulatorDisplayFloat = from;
            _accumulatorCountTween = DOTween.To(
                () => _accumulatorDisplayFloat,
                x =>
                {
                    _accumulatorDisplayFloat = x;
                    accumulatorLabel.text = FormatSignedAmount(Mathf.RoundToInt(x));
                    accumulatorLabel.color = accumulatorTextColor;
                },
                (float)to,
                accumulatorCountDuration
            ).SetEase(Ease.OutQuart).SetUpdate(true).OnComplete(() =>
            {
                _accumulatorCountTween = null;
                var cb = _onAccumulatorCountComplete;
                _onAccumulatorCountComplete = null;
                cb?.Invoke();
            });
        }

        private void PublishPendingCompletions()
        {
            foreach (var id in _pendingCompletionBatchIds)
            {
                EventBus.Publish(new GameEvents.SettlementPlaybackCompleted { BatchId = id });
            }
            _pendingCompletionBatchIds.Clear();
        }

        private void FlyAccumulatorToTopBar(Action onCompleted)
        {
            if (accumulatorRoot == null || accumulatorGroup == null || UIManager.Instance == null)
            {
                onCompleted?.Invoke();
                return;
            }

            var target = UIManager.Instance.ResolveMoneyTargetAnchor();
            if (target == null)
            {
                HideAccumulatorImmediate();
                onCompleted?.Invoke();
                return;
            }

            if (accumulatorLabel != null)
            {
                accumulatorLabel.text = FormatSignedAmount(_accumulatedAmount);
            }

            Vector2 targetPos = ResolveScreenPoint(target, Vector2.zero);

            _accumulatorFlySequence?.Kill(false);
            _accumulatorFlySequence = DOTween.Sequence().SetUpdate(true);
            _accumulatorFlySequence.Append(accumulatorRoot.DOAnchorPos(targetPos, accumulatorFlyDuration).SetEase(Ease.InCubic));
            _accumulatorFlySequence.Join(
                accumulatorGroup.DOFade(0f, accumulatorFlyDuration * 0.6f)
                    .SetEase(Ease.InQuad)
                    .SetDelay(accumulatorFlyDuration * 0.4f));
            _accumulatorFlySequence.OnComplete(() =>
            {
                HideAccumulatorImmediate();
                onCompleted?.Invoke();
            });
        }
    }
}

using System;
using System.Collections.Generic;
using BaoZuPo.Core;
using BaoZuPo.GameFlow;
using BaoZuPo.Integration.Martian.Feedback;
using BaoZuPo.Economy;
using Martian.EventBus;
using Martian.Feedback;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BaoZuPo.UI.Settlement
{
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

        private readonly Queue<UISettlementPlaybackBatch> _pendingBatches = new();
        private Sequence _activeTransferSequence;
        private Canvas _canvas;
        private RectTransform _canvasRect;
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
            if (batch == null || batch.IsEmpty)
            {
                return;
            }

            _pendingBatches.Enqueue(batch);

            if (UIManager.Instance != null && !UIManager.Instance.IsDeferredMoneyDisplayActive)
            {
                UIManager.Instance.BeginDeferredMoneyDisplay(MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0);
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
            PlayBatch(batch, OnBatchCompleted);
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
                PlayStage(stages, index + 1, onCompleted);
                return;
            }

            switch (stage.Kind)
            {
                case UISettlementPlaybackStageKind.Parallel:
                    PlayParallel(stage.Entries, () => PlayStage(stages, index + 1, onCompleted));
                    break;
                case UISettlementPlaybackStageKind.Barrier:
                    PlayStage(stages, index + 1, onCompleted);
                    break;
                case UISettlementPlaybackStageKind.Aggregate:
                case UISettlementPlaybackStageKind.Serial:
                default:
                    PlaySerial(stage.Entries, 0, () => PlayStage(stages, index + 1, onCompleted));
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
            PlayEntry(entry, () => PlaySerial(entries, index + 1, onCompleted));
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

            var handle = BaoZuPoFeedbackAdapter.PublishSettlementSequence(entry.Payload, entry.LaneKey);
            if (handle == null || handle.IsFinished)
            {
                PlayTransfer(entry, onCompleted);
                return;
            }

            void TriggerTransfer(FeedbackPlaybackHandle _)
            {
                handle.Completed -= TriggerTransfer;
                handle.Cancelled -= TriggerTransfer;
                PlayTransfer(entry, onCompleted);
            }

            handle.Completed += TriggerTransfer;
            handle.Cancelled += TriggerTransfer;

            if (handle.IsFinished)
            {
                handle.Completed -= TriggerTransfer;
                handle.Cancelled -= TriggerTransfer;
                PlayTransfer(entry, onCompleted);
            }
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
            if (entry != null && entry.Payload != null && !string.IsNullOrWhiteSpace(entry.Payload.BatchId))
            {
                EventBus.Publish(new GameEvents.SettlementPlaybackCompleted
                {
                    BatchId = entry.Payload.BatchId
                });
            }

            onCompleted?.Invoke();
        }

        private void OnBatchCompleted()
        {
            _isPlaybackRunning = false;
            if (_pendingBatches.Count > 0)
            {
                TryStartPlayback();
                return;
            }

            FinishPlayback();
        }

        private void FinishPlayback()
        {
            if (_isPlaybackRunning)
            {
                return;
            }

            if (_pendingBatches.Count > 0 || _activeTransferSequence != null)
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
                transferLabel.font = UIFontCatalog.GetPreferredFontAsset();
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

        private static string FormatSignedAmount(int amount)
        {
            string sign = amount > 0 ? "+" : string.Empty;
            return $"{sign}{amount}";
        }
    }
}

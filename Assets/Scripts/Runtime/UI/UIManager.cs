using System;
using System.Collections.Generic;
using BaoZuPo.Core;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using BaoZuPo.Integration.Martian.Feedback;
using BaoZuPo.Integration.Martian.Tooltip;
using BaoZuPo.UI.Settlement;
using DG.Tweening;
using Martian.EventBus;
using Martian.Tooltip;
using UnityEngine;

namespace BaoZuPo.UI
{
    /// <summary>
    /// UI 管理器，协调所有 UI 面板的生命周期和交互。
    /// 订阅游戏事件（阶段变化、卡牌打出、回合开始、游戏结束等）并驱动 UI 刷新。
    /// 管理结算演示序列、卡牌奖励选择、反馈启动等核心 UI 流程。
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("Panel References")]
        public UITopBar topBar;
        public UIHandPanel handPanel;
        public UIBoardPanel boardPanel;
        public UIPhasePanel phasePanel;
        public UIGameOverPanel gameOverPanel;
        public UICardDragController cardDragController;

        public GamePhase CurrentPhase { get; private set; } = GamePhase.Prepare;

        [SerializeField] private UISettlementSequenceController _settlementSequenceController;
        [SerializeField] private UICardRewardPanel _cardRewardPanel;
        [SerializeField] private UICardShopPanel _cardShopPanel;
        private bool _isUiReady;
        private bool _hasPendingTurnStarted;
        private GameEvents.TurnStarted _pendingTurnStarted;

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Subscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Subscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Subscribe<GameEvents.GameOver>(OnGameOver);
            EventBus.Subscribe<GameEvents.CardRewardOffered>(OnCardRewardOffered);
            EventBus.Subscribe<GameEvents.ShopOpened>(OnShopOpened);
            EventBus.Subscribe<GameEvents.ShopClosed>(OnShopClosed);
        }

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                CurrentPhase = TurnManager.Instance.CurrentPhase;
            }

            BaoZuPoMartianTooltipIntegration.Install();
            ConfigureFeedbackSettings();
            ValidateRequiredSceneReferences();
            InitializeCardDragController();
            RefreshAll();
            phasePanel?.UpdatePhase(CurrentPhase.ToString());
            _isUiReady = true;

            if (_hasPendingTurnStarted)
            {
                HandleTurnStarted(_pendingTurnStarted);
                _hasPendingTurnStarted = false;
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Unsubscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Unsubscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Unsubscribe<GameEvents.GameOver>(OnGameOver);
            EventBus.Unsubscribe<GameEvents.CardRewardOffered>(OnCardRewardOffered);
            EventBus.Unsubscribe<GameEvents.ShopOpened>(OnShopOpened);
            EventBus.Unsubscribe<GameEvents.ShopClosed>(OnShopClosed);
        }

        private new void OnApplicationQuit()
        {
            DOTween.KillAll(false);
        }

        private void OnPhaseChanged(GameEvents.PhaseChanged e)
        {
            CurrentPhase = e.Phase;
            if (!string.IsNullOrWhiteSpace(e.PhaseName) && Enum.TryParse(e.PhaseName, true, out GamePhase parsedPhase))
            {
                CurrentPhase = parsedPhase;
            }

            ResetTransientInteraction();
            if (e.Phase == GamePhase.Settle)
            {
                BeginDeferredMoneyDisplay(MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0);
            }

            string phaseLabel = string.IsNullOrWhiteSpace(e.PhaseName) ? CurrentPhase.ToString() : e.PhaseName;
            phasePanel?.UpdatePhase(phaseLabel);
            RefreshHudAndHand();
        }

        private void OnCardPlayed(GameEvents.CardPlayed e)
        {
            ResetTransientInteraction();
            RefreshAll();
        }

        private void OnTurnStarted(GameEvents.TurnStarted e)
        {
            if (!_isUiReady)
            {
                _pendingTurnStarted = e;
                _hasPendingTurnStarted = true;
                return;
            }

            HandleTurnStarted(e);
        }

        private void HandleTurnStarted(GameEvents.TurnStarted e)
        {
            ResetTransientInteraction();
            topBar?.RefreshTurn(e.TurnNumber);

            int turnsUntilLoanDue = 0;
            bool isLoanDueToday = false;
            if (TurnManager.Instance != null
                && TurnManager.Instance.TryGetNextLoanPreview(out int dueTurn, out _))
            {
                turnsUntilLoanDue = Mathf.Max(0, dueTurn - e.TurnNumber);
                isLoanDueToday = turnsUntilLoanDue == 0;
            }

            BaoZuPoFeedbackAdapter.PublishTurnStartReminders(e.TurnNumber, turnsUntilLoanDue, isLoanDueToday);
            RefreshHudAndHand();
        }

        private void OnGameOver(GameEvents.GameOver e)
        {
            ResetTransientInteraction();
            int totalEarnedMoney = e.FinalMoney + (MoneyManager.Instance != null ? MoneyManager.Instance.TotalSpent : 0);
            gameOverPanel?.Show(e.TotalTurns, totalEarnedMoney);
        }

        private void OnCardRewardOffered(GameEvents.CardRewardOffered e)
        {
            if (_cardRewardPanel == null)
            {
                throw new InvalidOperationException("[UIManager] _cardRewardPanel is not assigned in the Inspector. Reward selection is required for the main gameplay flow.");
            }

            _cardRewardPanel.Show(e.Options, e.Boosted);
        }

        private void OnShopOpened(GameEvents.ShopOpened e)
        {
            if (_cardShopPanel == null)
            {
                throw new InvalidOperationException("[UIManager] _cardShopPanel is not assigned in the Inspector. Shop flow is required for the main gameplay flow.");
            }

            _cardShopPanel.Show(e.Options);
        }

        private void OnShopClosed(GameEvents.ShopClosed _)
        {
            _cardShopPanel?.Hide();
        }

        private void ResetTransientInteraction()
        {
            cardDragController?.CancelCurrentDrag(true);
            TooltipServices.Current.HideAll();
        }

        public void RefreshAll()
        {
            ResetTransientInteraction();
            RefreshHudAndHand();
            boardPanel?.RefreshBoard();
        }

        private void RefreshHudAndHand()
        {
            topBar?.Refresh();
            // 阶段切换/回合开始只更新 HUD 和手牌，避免无意义重建房间 UI 造成一帧叠影闪烁。
            handPanel?.RefreshHand();
        }

        public void BeginDeferredMoneyDisplay(int startValue)
        {
            topBar?.BeginDeferredMoneyDisplay(startValue);
        }

        public void CommitDisplayedDelta(int delta)
        {
            topBar?.CommitDisplayedDelta(delta);
        }

        public void EndDeferredMoneyDisplay(System.Action onCountUpComplete = null)
        {
            topBar?.EndDeferredMoneyDisplay(onCountUpComplete);
        }

        public RectTransform ResolveMoneyTargetAnchor()
        {
            return topBar != null ? topBar.MoneyTargetAnchor : null;
        }

        public bool IsDeferredMoneyDisplayActive => topBar != null && topBar.IsDeferredMoneyDisplayActive;

        public void SubmitSettlementPayload(GameEvents.SettlementSequenceQueued payload)
        {
            _settlementSequenceController?.Queue(payload);
        }

        public void SubmitSettlementPayloads(IReadOnlyList<GameEvents.SettlementSequenceQueued> payloads)
        {
            _settlementSequenceController?.QueueBatch(payloads);
        }

        public void SubmitSettlementBatch(UISettlementPlaybackBatch batch)
        {
            _settlementSequenceController?.Queue(batch);
        }

        private void InitializeCardDragController()
        {
            if (cardDragController == null)
            {
                Debug.LogError("[UIManager] cardDragController is not assigned in the Inspector. Create a child object under UIManager and add UICardDragController.");
                return;
            }

            cardDragController.BindDragLayer(null);
        }

        private void ValidateRequiredSceneReferences()
        {
            if (_cardRewardPanel == null)
            {
                throw new InvalidOperationException("[UIManager] _cardRewardPanel is not assigned in the Inspector. Reward selection is required for the main gameplay flow.");
            }

            if (_cardShopPanel == null)
            {
                throw new InvalidOperationException("[UIManager] _cardShopPanel is not assigned in the Inspector. Shop flow is required for the main gameplay flow.");
            }
        }

        private void ConfigureFeedbackSettings()
        {
            BaoZuPoMartianFeedbackIntegration.Configure(
                GameManager.Instance != null ? GameManager.Instance.gameConfig : null);
        }
    }
}

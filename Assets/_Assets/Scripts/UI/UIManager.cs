using System;
using System.Collections.Generic;
using BaoZuPo.Core;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using BaoZuPo.Integration.Martian.Feedback;
using BaoZuPo.Integration.Martian.Tooltip;
using BaoZuPo.UI.Settlement;
using Martian.EventBus;
using Martian.Feedback.Runtime;
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

        [SerializeField] private FeedbackBootstrap _feedbackBootstrap;
        [SerializeField] private UISettlementSequenceController _settlementSequenceController;
        [SerializeField] private UICardRewardPanel _cardRewardPanel;

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Subscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Subscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Subscribe<GameEvents.GameOver>(OnGameOver);
            EventBus.Subscribe<GameEvents.CardRewardOffered>(OnCardRewardOffered);
        }

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                CurrentPhase = TurnManager.Instance.CurrentPhase;
            }

            BaoZuPoMartianTooltipIntegration.Install();
            ConfigureFeedbackBootstrap();
            ValidateRequiredSceneReferences();
            InitializeCardDragController();
            RefreshAll();
            phasePanel?.UpdatePhase(CurrentPhase.ToString());
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Unsubscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Unsubscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Unsubscribe<GameEvents.GameOver>(OnGameOver);
            EventBus.Unsubscribe<GameEvents.CardRewardOffered>(OnCardRewardOffered);
        }

        private void OnPhaseChanged(GameEvents.PhaseChanged e)
        {
            CurrentPhase = e.Phase;
            if (!string.IsNullOrWhiteSpace(e.PhaseName) && Enum.TryParse(e.PhaseName, true, out GamePhase parsedPhase))
            {
                CurrentPhase = parsedPhase;
            }

            cardDragController?.CancelCurrentDrag(true);
            TooltipServices.Current.HideAll();
            if (e.Phase == GamePhase.Settle)
            {
                BeginDeferredMoneyDisplay(MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0);
            }

            phasePanel?.UpdatePhase(string.IsNullOrWhiteSpace(e.PhaseName) ? CurrentPhase.ToString() : e.PhaseName);
            RefreshAll();
        }

        private void OnCardPlayed(GameEvents.CardPlayed e)
        {
            cardDragController?.CancelCurrentDrag(true);
            TooltipServices.Current.HideAll();
            RefreshAll();
        }

        private void OnTurnStarted(GameEvents.TurnStarted e)
        {
            cardDragController?.CancelCurrentDrag(true);
            TooltipServices.Current.HideAll();
            topBar?.RefreshTurn(e.TurnNumber);
            RefreshAll();
        }

        private void OnGameOver(GameEvents.GameOver e)
        {
            cardDragController?.CancelCurrentDrag(true);
            TooltipServices.Current.HideAll();
            gameOverPanel?.Show(e.TotalTurns, e.FinalMoney);
        }

        private void OnCardRewardOffered(GameEvents.CardRewardOffered e)
        {
            if (_cardRewardPanel == null)
            {
                throw new InvalidOperationException("[UIManager] _cardRewardPanel is not assigned in the Inspector. Reward selection is required for the main gameplay flow.");
            }

            _cardRewardPanel.Show(e.Options, e.Boosted);
        }

        public void RefreshAll()
        {
            cardDragController?.CancelCurrentDrag(true);
            TooltipServices.Current.HideAll();
            topBar?.Refresh();
            handPanel?.RefreshHand();
            boardPanel?.RefreshBoard();
        }

        public void BeginDeferredMoneyDisplay(int startValue)
        {
            topBar?.BeginDeferredMoneyDisplay(startValue);
        }

        public void CommitDisplayedDelta(int delta)
        {
            topBar?.CommitDisplayedDelta(delta);
        }

        public void EndDeferredMoneyDisplay()
        {
            topBar?.EndDeferredMoneyDisplay();
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
        }

        private void ConfigureFeedbackBootstrap()
        {
            if (_feedbackBootstrap == null)
            {
                Debug.LogError("[UIManager] _feedbackBootstrap is not assigned in the Inspector. Create a child object under UIManager and add FeedbackBootstrap.");
                return;
            }

            BaoZuPoMartianFeedbackIntegration.Configure(
                _feedbackBootstrap,
                GameManager.Instance != null ? GameManager.Instance.gameConfig : null);
        }
    }
}

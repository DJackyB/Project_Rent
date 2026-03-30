using System.Collections.Generic;
using BaoZuPo.Core;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using BaoZuPo.Integration.Martian.Feedback;
using BaoZuPo.Integration.Martian.Tooltip;
using BaoZuPo.Localization;
using BaoZuPo.UI.Settlement;
using Martian.EventBus;
using Martian.Feedback.Runtime;
using Martian.Localization;
using Martian.Tooltip;
using UnityEngine;

namespace BaoZuPo.UI
{
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

        private void OnEnable()
        {
            BaoZuPoLocalizationBootstrap.EnsureInitialized();
            EventBus.Subscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Subscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Subscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Subscribe<GameEvents.GameOver>(OnGameOver);
            LocalizationServices.Language.LanguageChanged += OnLanguageChanged;
        }

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                CurrentPhase = TurnManager.Instance.CurrentPhase;
            }

            BaoZuPoMartianTooltipIntegration.Install();
            ConfigureFeedbackBootstrap();
            InitializeCardDragController();
            RefreshAll();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Unsubscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Unsubscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Unsubscribe<GameEvents.GameOver>(OnGameOver);
            LocalizationServices.Language.LanguageChanged -= OnLanguageChanged;
        }

        private void OnPhaseChanged(GameEvents.PhaseChanged e)
        {
            CurrentPhase = e.Phase;
            if (!string.IsNullOrWhiteSpace(e.PhaseName) && System.Enum.TryParse(e.PhaseName, true, out GamePhase parsedPhase))
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

        private void OnLanguageChanged(string _)
        {
            LocalizationFontUtility.ApplyToAllLoadedSceneTexts();
            RefreshAll();
            phasePanel?.UpdatePhase(CurrentPhase.ToString());
            gameOverPanel?.RefreshLocalization();
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
                Debug.LogError("[UIManager] cardDragController is not assigned in the Inspector. Please create a child object under UIManager and add UICardDragController.");
                return;
            }

            cardDragController.BindDragLayer(null);
        }

        private void ConfigureFeedbackBootstrap()
        {
            if (_feedbackBootstrap == null)
            {
                Debug.LogError("[UIManager] _feedbackBootstrap is not assigned in the Inspector. Please create a child object under UIManager and add FeedbackBootstrap.");
                return;
            }

            BaoZuPoMartianFeedbackIntegration.Configure(
                _feedbackBootstrap,
                GameManager.Instance != null ? GameManager.Instance.gameConfig : null);
        }
    }
}

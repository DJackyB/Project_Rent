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
            EventBus.Subscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Subscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Subscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Subscribe<GameEvents.GameOver>(OnGameOver);
            EventBus.Subscribe<GameEvents.GameStateLoaded>(OnGameStateLoaded);
            LocalizationManager.LanguageChanged += OnLanguageChanged;
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
            phasePanel?.UpdatePhase(CurrentPhase.ToString());
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Unsubscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Unsubscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Unsubscribe<GameEvents.GameOver>(OnGameOver);
            EventBus.Unsubscribe<GameEvents.GameStateLoaded>(OnGameStateLoaded);
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
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

        private void OnGameStateLoaded(GameEvents.GameStateLoaded e)
        {
            CurrentPhase = e.Phase;
            cardDragController?.CancelCurrentDrag(true);
            TooltipServices.Current.HideAll();
            phasePanel?.UpdatePhase(CurrentPhase.ToString());

            if (e.IsGameOver)
            {
                gameOverPanel?.Show(e.TurnNumber, MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0);
            }
            else
            {
                gameOverPanel?.Hide();
            }

            RefreshAll();
        }

        private void OnLanguageChanged()
        {
            UIFontCatalog.ApplyToAllLoadedSceneTexts();
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
        public bool IsSettlementPlaybackBusy => _settlementSequenceController != null && _settlementSequenceController.IsPlaybackBusy;

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

        public void PrepareForGameplayLoad()
        {
            cardDragController?.CancelCurrentDrag(true);
            TooltipServices.Current.HideAll();
            _settlementSequenceController?.CancelPlaybackImmediately();
            _feedbackBootstrap?.Coordinator?.Clear();
            EndDeferredMoneyDisplay();
            gameOverPanel?.Hide();
        }

        private void InitializeCardDragController()
        {
            if (cardDragController == null)
            {
                Debug.LogError("[UIManager] cardDragController 未在 Inspector 中赋值。请在 UIManager 下创建子对象并挂载 UICardDragController 组件。");
                return;
            }

            cardDragController.BindDragLayer(null);
        }

        private void ConfigureFeedbackBootstrap()
        {
            if (_feedbackBootstrap == null)
            {
                Debug.LogError("[UIManager] _feedbackBootstrap 未在 Inspector 中赋值。请在 UIManager 下创建子对象并挂载 FeedbackBootstrap 组件。");
                return;
            }

            BaoZuPoMartianFeedbackIntegration.Configure(
                _feedbackBootstrap,
                GameManager.Instance != null ? GameManager.Instance.gameConfig : null);
        }
    }
}

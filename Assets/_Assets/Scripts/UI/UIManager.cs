using BaoZuPo.Core;
using BaoZuPo.GameFlow;
using BaoZuPo.UI.Common.Tooltip;
using BaoZuPo.UI.Settlement;
using Martian.EventBus;
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

        private UISettlementSequenceController _settlementSequenceController;

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Subscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
            EventBus.Subscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Subscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Subscribe<GameEvents.GameOver>(OnGameOver);
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                CurrentPhase = TurnManager.Instance.CurrentPhase;
            }

            EnsureCardDragController();
            EnsureSettlementSequenceController();
            RefreshAll();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Unsubscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
            EventBus.Unsubscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Unsubscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Unsubscribe<GameEvents.GameOver>(OnGameOver);
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
            phasePanel?.UpdatePhase(string.IsNullOrWhiteSpace(e.PhaseName) ? CurrentPhase.ToString() : e.PhaseName);
            RefreshAll();
        }

        private void OnMoneyChanged(GameEvents.MoneyChanged e)
        {
            topBar?.RefreshMoney(e.NewValue);
            topBar?.RefreshSummary();
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

        private void EnsureCardDragController()
        {
            if (cardDragController != null)
            {
                return;
            }

            var controllerTransform = transform.Find("CardDragController");
            if (controllerTransform == null)
            {
                var controllerObject = new GameObject("CardDragController", typeof(RectTransform));
                controllerObject.transform.SetParent(transform, false);

                var rect = controllerObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                controllerTransform = controllerObject.transform;
            }

            cardDragController = controllerTransform.GetComponent<UICardDragController>();
            if (cardDragController == null)
            {
                cardDragController = controllerTransform.gameObject.AddComponent<UICardDragController>();
            }

            cardDragController.BindDragLayer(null);
        }

        private void EnsureSettlementSequenceController()
        {
            if (_settlementSequenceController != null)
            {
                return;
            }

            var controllerTransform = transform.Find("SettlementSequenceController");
            if (controllerTransform == null)
            {
                controllerTransform = new GameObject("SettlementSequenceController", typeof(RectTransform)).transform;
                controllerTransform.SetParent(transform, false);
            }

            _settlementSequenceController = controllerTransform.GetComponent<UISettlementSequenceController>();
            if (_settlementSequenceController == null)
            {
                _settlementSequenceController = controllerTransform.gameObject.AddComponent<UISettlementSequenceController>();
            }
        }
    }
}

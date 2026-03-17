using UnityEngine;
using BaoZuPo.Core;
using Martian.EventBus;

namespace BaoZuPo.UI
{
    /// <summary>
    /// UI 总管理器
    /// 挂在 Canvas 上，持有所有 UI 面板引用，监听事件刷新 UI。
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("UI 面板引用")]
        public UITopBar topBar;
        public UIHandPanel handPanel;
        public UIBoardPanel boardPanel;
        public UIPhasePanel phasePanel;
        public UIGameOverPanel gameOverPanel;

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Subscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
            EventBus.Subscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Subscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Subscribe<GameEvents.GameOver>(OnGameOver);
        }

        private void Start()
        {
            // 初始刷新
            RefreshAll();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Unsubscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
            EventBus.Unsubscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Unsubscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Unsubscribe<GameEvents.GameOver>(OnGameOver);
        }

        private void OnPhaseChanged(GameEvents.PhaseChanged e)
        {
            phasePanel?.UpdatePhase(e.PhaseName);
            RefreshAll();
        }

        private void OnMoneyChanged(GameEvents.MoneyChanged e)
        {
            topBar?.RefreshMoney(e.NewValue);
        }

        private void OnCardPlayed(GameEvents.CardPlayed e)
        {
            RefreshAll();
        }

        private void OnTurnStarted(GameEvents.TurnStarted e)
        {
            topBar?.RefreshTurn(e.TurnNumber);
            RefreshAll();
        }

        private void OnGameOver(GameEvents.GameOver e)
        {
            gameOverPanel?.Show(e.TotalTurns, e.FinalMoney);
        }

        /// <summary>
        /// 全量刷新所有面板
        /// </summary>
        public void RefreshAll()
        {
            topBar?.Refresh();
            handPanel?.RefreshHand();
            boardPanel?.RefreshBoard();
        }
    }
}

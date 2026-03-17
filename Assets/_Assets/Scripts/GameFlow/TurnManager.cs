using System.Collections.Generic;
using UnityEngine;
using BaoZuPo.Core;
using BaoZuPo.Card;
using BaoZuPo.Board;
using BaoZuPo.Economy;
using Martian.EventBus;

namespace BaoZuPo.GameFlow
{
    /// <summary>
    /// 回合管理器
    /// 提供各阶段的核心逻辑，由 NodeCanvas FSM 的 Action 节点调用。
    /// </summary>
    public class TurnManager : Singleton<TurnManager>
    {
        [Header("运行时信息")]
        [SerializeField] private int _currentTurn = 0;

        /// <summary>当前回合数</summary>
        public int CurrentTurn => _currentTurn;

        /// <summary>玩家是否已结束行动阶段</summary>
        public bool ActionPhaseEnded { get; set; }

        // ==================== 准备阶段 ====================

        /// <summary>
        /// 执行准备阶段逻辑：
        /// 1. 回合数 +1
        /// 2. 抽牌
        /// 3. 场上卡牌耐久 -1，归零触发销毁效果
        /// 4. 触发所有前置效果
        /// </summary>
        public void ExecutePreparePhase()
        {
            _currentTurn++;
            Debug.Log($"===== 第 {_currentTurn} 回合 · 准备阶段 =====");

            EventBus.Publish(new GameEvents.TurnStarted { TurnNumber = _currentTurn });
            EventBus.Publish(new GameEvents.PhaseChanged { PhaseName = "Prepare" });

            // 抽牌
            var config = GameManager.Instance.gameConfig;
            Deck.DeckManager.Instance.Draw(config.drawCount);

            // 处理场上所有卡牌
            var fieldCards = BoardManager.Instance.GetAllFieldCards();
            var toDestroy = new List<CardInstance>();

            foreach (var card in fieldCards)
            {
                if (card.IsDestroyed) continue;

                // 耐久 -1（0 表示无限耐久）
                if (card.Data.durability > 0)
                {
                    card.CurrentDurability--;
                    if (card.CurrentDurability <= 0)
                    {
                        toDestroy.Add(card);
                        continue;
                    }
                }

                // 触发前置效果
                card.PreEffect?.Execute(card, GameManager.Instance.GameContext);
            }

            // 处理销毁
            foreach (var card in toDestroy)
            {
                Debug.Log($"[TurnManager] 卡牌耐久归零销毁: {card}");
                card.DestroyEffect?.Execute(card, GameManager.Instance.GameContext);
                card.MarkDestroyed();

                EventBus.Publish(new GameEvents.CardDestroyed
                {
                    Card = card,
                    TriggeredByDurability = true
                });
            }

            BoardManager.Instance.CleanupDestroyedCards();
        }

        // ==================== 行动阶段 ====================

        /// <summary>
        /// 开始行动阶段
        /// 重置行动状态，等待玩家出牌。
        /// </summary>
        public void StartActionPhase()
        {
            Debug.Log($"===== 第 {_currentTurn} 回合 · 行动阶段 =====");
            ActionPhaseEnded = false;

            EventBus.Publish(new GameEvents.PhaseChanged { PhaseName = "Action" });
        }

        /// <summary>
        /// 玩家打出一张牌到指定房间
        /// </summary>
        public bool PlayCard(CardInstance card, RoomSlot targetRoom)
        {
            var context = GameManager.Instance.GameContext;

            // 检查费用
            if (!MoneyManager.Instance.CanAfford(card.Data.cost))
            {
                Debug.LogWarning($"[TurnManager] 无法支付 {card.Data.cardName} 的费用: {card.Data.cost}");
                return false;
            }

            // 事件卡不需要放置到房间
            if (card.Data.cardType == CardType.Event)
            {
                MoneyManager.Instance.ReduceMoney(card.Data.cost);
                card.InstantEffect?.Execute(card, context);
                Deck.DeckManager.Instance.RemoveFromHand(card);

                EventBus.Publish(new GameEvents.CardPlayed { Card = card });
                Debug.Log($"[TurnManager] 打出事件卡: {card}");
                return true;
            }

            // 放置到房间
            if (targetRoom == null || !targetRoom.PlaceCard(card))
            {
                Debug.LogWarning($"[TurnManager] 无法放置卡牌到房间: {card}");
                return false;
            }

            MoneyManager.Instance.ReduceMoney(card.Data.cost);
            card.InstantEffect?.Execute(card, context);
            Deck.DeckManager.Instance.RemoveFromHand(card);

            EventBus.Publish(new GameEvents.CardPlayed { Card = card });
            Debug.Log($"[TurnManager] 打出卡牌: {card} -> 房间 {targetRoom.RoomIndex}");
            return true;
        }

        /// <summary>
        /// 结束行动阶段（玩家手动点击结束）
        /// </summary>
        public void EndActionPhase()
        {
            ActionPhaseEnded = true;
            Debug.Log("[TurnManager] 玩家结束行动阶段");
        }

        // ==================== 结算阶段 ====================

        /// <summary>
        /// 执行结算阶段逻辑：
        /// 1. 触发场上所有卡牌的结算效果
        /// 2. 等待倒计时 -1，归零则移除（不触发销毁效果）
        /// 3. 还贷判定
        /// </summary>
        public void ExecuteSettlePhase()
        {
            Debug.Log($"===== 第 {_currentTurn} 回合 · 结算阶段 =====");
            EventBus.Publish(new GameEvents.PhaseChanged { PhaseName = "Settle" });

            var context = GameManager.Instance.GameContext;
            var fieldCards = BoardManager.Instance.GetAllFieldCards();
            var toRemove = new List<CardInstance>();

            foreach (var card in fieldCards)
            {
                if (card.IsDestroyed) continue;

                // 触发结算效果
                card.SettleEffect?.Execute(card, context);

                // 等待倒计时 -1（0 表示无等待）
                if (card.Data.waitTurns > 0)
                {
                    card.CurrentWait--;
                    if (card.CurrentWait <= 0)
                    {
                        toRemove.Add(card);
                    }
                }
            }

            // 等待归零的卡牌：移除但不触发销毁效果
            foreach (var card in toRemove)
            {
                Debug.Log($"[TurnManager] 卡牌等待归零移除: {card}");
                card.MarkDestroyed();
                EventBus.Publish(new GameEvents.CardDestroyed
                {
                    Card = card,
                    TriggeredByDurability = false
                });
            }

            BoardManager.Instance.CleanupDestroyedCards();

            // 还贷判定
            var config = GameManager.Instance.gameConfig;
            if (config.loanInterval > 0 && _currentTurn % config.loanInterval == 0)
            {
                bool paid = MoneyManager.Instance.ReduceMoney(config.loanAmount);
                EventBus.Publish(new GameEvents.LoanPayment
                {
                    Amount = config.loanAmount,
                    RemainingMoney = MoneyManager.Instance.CurrentMoney
                });

                if (!paid)
                {
                    Debug.LogError($"[TurnManager] 还贷失败！游戏结束！");
                    EventBus.Publish(new GameEvents.GameOver
                    {
                        FinalMoney = MoneyManager.Instance.CurrentMoney,
                        TotalTurns = _currentTurn
                    });
                }
            }

            EventBus.Publish(new GameEvents.TurnEnded { TurnNumber = _currentTurn });
        }
    }
}

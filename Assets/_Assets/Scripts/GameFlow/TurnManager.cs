using System.Collections.Generic;
using UnityEngine;
using BaoZuPo.Core;
using BaoZuPo.Card;
using BaoZuPo.Board;
using BaoZuPo.Economy;
using Martian.EventBus;
using System.Linq;

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
        [SerializeField] private bool _isGameOver = false;
        [SerializeField] private int _loanPaymentCount = 0;

        /// <summary>当前回合数</summary>
        public int CurrentTurn => _currentTurn;

        /// <summary>游戏是否已经结束</summary>
        public bool IsGameOver => _isGameOver;

        /// <summary>玩家是否已结束行动阶段</summary>
        public bool ActionPhaseEnded { get; set; }

        // ==================== 准备阶段 ====================

        /// <summary>
        /// 执行准备阶段逻辑：
        /// 1. 回合数 +1
        /// 2. 触发场上卡牌前置效果（含合同）
        /// 3. 抽牌（首回合与后续回合数量不同）
        /// </summary>
        public void ExecutePreparePhase()
        {
            if (_isGameOver) return;

            _currentTurn++;
            Debug.Log($"===== 第 {_currentTurn} 回合 · 准备阶段 =====");

            EventBus.Publish(new GameEvents.TurnStarted { TurnNumber = _currentTurn });
            EventBus.Publish(new GameEvents.PhaseChanged { PhaseName = "Prepare" });

            // 先处理场上所有卡牌的前置效果（v0.6 规则，含合同）
            var fieldCards = BoardManager.Instance.GetAllFieldCards();
            foreach (var card in fieldCards)
            {
                if (card.IsDestroyed) continue;
                card.PreEffect?.Execute(card, GameManager.Instance.GameContext);
            }

            // 再抽牌（首回合与后续回合数量不同）
            var config = GameManager.Instance.gameConfig;
            int drawCount = _currentTurn == 1 ? config.firstTurnDrawCount : config.normalTurnDrawCount;
            Deck.DeckManager.Instance.Draw(drawCount);

            BoardManager.Instance.CleanupDestroyedCards();
        }

        // ==================== 行动阶段 ====================

        /// <summary>
        /// 开始行动阶段
        /// 重置行动状态，等待玩家出牌。
        /// </summary>
        public void StartActionPhase()
        {
            if (_isGameOver) return;

            Debug.Log($"===== 第 {_currentTurn} 回合 · 行动阶段 =====");
            ActionPhaseEnded = false;

            EventBus.Publish(new GameEvents.PhaseChanged { PhaseName = "Action" });
        }

        /// <summary>
        /// 玩家打出一张牌到指定房间
        /// </summary>
        public bool PlayCard(CardInstance card, RoomSlot targetRoom)
        {
            if (_isGameOver) return false;

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

            // 合同卡生效后常驻，不占用房间槽位
            if (card.Data.cardType == CardType.Contract)
            {
                MoneyManager.Instance.ReduceMoney(card.Data.cost);
                card.InstantEffect?.Execute(card, context);
                Deck.DeckManager.Instance.RemoveFromHand(card);
                BoardManager.Instance.AddContract(card);

                EventBus.Publish(new GameEvents.CardPlayed { Card = card });
                Debug.Log($"[TurnManager] 打出合同卡: {card}");
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
            if (_isGameOver) return;

            ActionPhaseEnded = true;
            Debug.Log("[TurnManager] 玩家结束行动阶段");
        }

        // ==================== 结算阶段 ====================

        /// <summary>
        /// 执行结算阶段逻辑：
        /// 1. 房间结算（有租客房间）+ 房间耐久结算
        /// 2. 合同结算 + 合同耐久结算
        /// 3. 等待倒计时（场上 + 手牌）
        /// 4. 扣款判定（指数增长）
        /// 5. 新牌入库（三选一）
        /// </summary>
        public void ExecuteSettlePhase()
        {
            if (_isGameOver) return;

            Debug.Log($"===== 第 {_currentTurn} 回合 · 结算阶段 =====");
            EventBus.Publish(new GameEvents.PhaseChanged { PhaseName = "Settle" });

            var context = GameManager.Instance.GameContext;
            var toRemove = new List<CardInstance>();
            var toDestroy = new List<CardInstance>();

            // 1) 收租：逐房间结算（仅有租客的房间结算）
            var rooms = BoardManager.Instance.GetAllRooms();
            foreach (var room in rooms)
            {
                if (room.TenantCount <= 0) continue;

                var roomCards = room.GetAllCards();
                foreach (var card in roomCards)
                {
                    if (card.IsDestroyed) continue;
                    card.SettleEffect?.Execute(card, context);
                }

                // 房间结算后：耐久-1，耐久归零触发销毁效果
                foreach (var card in roomCards)
                {
                    if (card.IsDestroyed) continue;
                    if (card.Data.durability <= 0) continue; // 0 表示无限耐久

                    card.CurrentDurability--;
                    if (card.CurrentDurability <= 0)
                    {
                        toDestroy.Add(card);
                    }
                }
            }

            // 2) 合同牌结算与耐久
            var contracts = BoardManager.Instance.GetAllContracts();
            foreach (var contract in contracts)
            {
                if (contract.IsDestroyed) continue;
                contract.SettleEffect?.Execute(contract, context);

                if (contract.Data.durability <= 0) continue;
                contract.CurrentDurability--;
                if (contract.CurrentDurability <= 0)
                {
                    toDestroy.Add(contract);
                }
            }

            // 3) 场上等待倒计时（等待归零移除，不触发销毁效果）
            var fieldCards = BoardManager.Instance.GetAllFieldCards();
            foreach (var card in fieldCards)
            {
                if (card.IsDestroyed) continue;

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

            // 处理耐久归零销毁（触发销毁效果）
            foreach (var card in toDestroy)
            {
                if (card.IsDestroyed) continue;

                Debug.Log($"[TurnManager] 卡牌耐久归零销毁: {card}");
                card.DestroyEffect?.Execute(card, context);
                card.MarkDestroyed();
                EventBus.Publish(new GameEvents.CardDestroyed
                {
                    Card = card,
                    TriggeredByDurability = true
                });
            }

            // 等待归零的卡牌：移除但不触发销毁效果
            foreach (var card in toRemove)
            {
                if (card.IsDestroyed) continue;

                Debug.Log($"[TurnManager] 卡牌等待归零移除: {card}");
                card.MarkDestroyed();
                EventBus.Publish(new GameEvents.CardDestroyed
                {
                    Card = card,
                    TriggeredByDurability = false
                });
            }

            BoardManager.Instance.CleanupDestroyedCards();

            // 回合结束：处理手牌等待倒计时，归零进入弃牌堆
            Deck.DeckManager.Instance.ResolveHandWaitAndDiscardExpired();

            // 还贷判定
            var config = GameManager.Instance.gameConfig;
            if (config.loanInterval > 0 && _currentTurn % config.loanInterval == 0)
            {
                int requiredPayment = CalculateCurrentLoanPayment(config.loanAmount, config.loanGrowthFactor);
                bool paid = MoneyManager.Instance.ReduceMoney(requiredPayment);
                EventBus.Publish(new GameEvents.LoanPayment
                {
                    Amount = requiredPayment,
                    RemainingMoney = MoneyManager.Instance.CurrentMoney
                });

                if (!paid)
                {
                    Debug.LogError($"[TurnManager] 还贷失败！游戏结束！");
                    _isGameOver = true;
                    EventBus.Publish(new GameEvents.GameOver
                    {
                        FinalMoney = MoneyManager.Instance.CurrentMoney,
                        TotalTurns = _currentTurn
                    });
                }
                else
                {
                    _loanPaymentCount++;
                }
            }

            // 4) 新牌入库（三选一）- 当前为无 UI 的自动选择实现
            if (!_isGameOver)
            {
                bool boosted = config.loanInterval > 0 && _currentTurn % config.loanInterval == 0;
                AwardOneCardFromThreeOptions(boosted);
            }

            EventBus.Publish(new GameEvents.TurnEnded { TurnNumber = _currentTurn });
        }

        private int CalculateCurrentLoanPayment(int baseAmount, float growthFactor)
        {
            int safeBase = Mathf.Max(0, baseAmount);
            float safeFactor = Mathf.Max(1f, growthFactor);
            float raw = safeBase * Mathf.Pow(safeFactor, _loanPaymentCount);
            return Mathf.RoundToInt(raw);
        }

        private void AwardOneCardFromThreeOptions(bool boosted)
        {
            var allCards = CardDatabase.GetAll().Values;
            var source = boosted
                ? allCards.Where(c => c.rarity >= CardRarity.Rare).ToList()
                : allCards.ToList();

            if (source.Count == 0)
            {
                Debug.LogWarning("[TurnManager] 三选一奖励池为空，跳过本回合新牌入库");
                return;
            }

            var options = new List<CardData>();
            for (int i = 0; i < 3; i++)
            {
                var picked = source[Random.Range(0, source.Count)];
                options.Add(picked);
            }

            // 暂无三选一 UI，先随机选择一张加入手牌
            var chosen = options[Random.Range(0, options.Count)];
            Deck.DeckManager.Instance.AddCardToHand(chosen);

            Debug.Log($"[TurnManager] 三选一（自动）获得卡牌: {chosen.cardName}（boosted={boosted}）");
        }
    }
}

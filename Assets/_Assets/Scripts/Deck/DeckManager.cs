using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Core;
using UnityEngine;

namespace BaoZuPo.Deck
{
    /// <summary>
    /// 卡牌管理器：负责手牌、抽卡池、弃卡池的生命周期管理和抽卡逻辑。
    ///
    /// 职责：
    /// 1. 手牌管理：维护玩家手中的卡牌列表，限制手牌大小（默认7张）
    /// 2. 抽卡池管理：从CardLibrary随机抽取卡牌并创建CardInstance
    /// 3. 弃卡池管理：存储已使用的卡牌，等待洗牌
    /// 4. 卡牌生成：生成卡牌实例时使用工厂方法CreateCardInstance()
    /// 5. 过期卡管理：处理带等待回合数的卡牌（waitTurns > 0）
    /// 6. 清理机制：定期移除被摧毁的卡牌
    ///
    /// 核心流程：
    /// [初始化] → Initialize(maxHandSize)
    /// [抽卡] → Draw(count) → DrawFromLibrary(library, count)
    ///   └─ 从library随机选卡 → CreateCardInstance() → 放入_hand（受上限限制）
    /// [使用卡] → PlayCard(cardIndex) → RemoveFromHand()
    ///   └─ 卡牌效果处理由外部逻辑完成
    /// [过期卡] → ResolveHandWaitAndDiscardExpired() 每回合调用
    ///   └─ CurrentWait-- → 倒数完 → 从手牌清除
    /// [洗牌] → 当抽卡时库为空，将弃卡池作为新库重新抽（未在此实现，由外部调用）
    /// </summary>
    public class DeckManager : Core.Singleton<DeckManager>
    {
        [Header("Debug")]
        [SerializeField] private int _drawPileCount;       // 抽卡池还有多少张卡（信息用）
        [SerializeField] private int _handCount;           // 当前手牌数
        [SerializeField] private int _discardPileCount;    // 弃卡池卡牌数

        private readonly List<CardInstance> _hand = new();             // 当前手牌列表
        private readonly List<CardInstance> _discardPile = new();      // 弃卡池（已使用或过期的卡）

        private int _maxHandSize = 7;  // 手牌上限（默认7张，可由Initialize修改）

        public IReadOnlyList<CardInstance> Hand => _hand;
        public int DrawPileCount => _drawPileCount;
        public int HandCount => _hand.Count;
        public int DiscardPileCount => _discardPile.Count;
        public int MaxHandSize => _maxHandSize;

        /// <summary>
        /// 初始化卡牌系统。清空手牌和弃卡池，设置手牌上限。
        /// </summary>
        /// <param name="maxHandSize">手牌上限（默认7张）</param>
        public void Initialize(int maxHandSize = 7)
        {
            _hand.Clear();
            _discardPile.Clear();

            _maxHandSize = Mathf.Max(1, maxHandSize);  // 确保至少为1
            UpdateDebugInfo();
            Debug.Log($"[DeckManager] Initialized card zones. Hand size cap: {_maxHandSize}.");
        }

        /// <summary>
        /// 抽卡（简化接口）。从GameConfig的normalTurnDrawLibrary中抽取指定数量的卡牌。
        /// 此方法是Draw(int)的包装，实际抽卡逻辑在DrawFromLibrary()。
        /// </summary>
        /// <param name="count">要抽取的卡牌数</param>
        /// <returns>成功抽取的卡牌列表（可能少于count，因为手牌上限、库为空等原因）</returns>
        public List<CardInstance> Draw(int count)
        {
            CardLibrary library = GameManager.Instance != null && GameManager.Instance.gameConfig != null
                ? GameManager.Instance.gameConfig.normalTurnDrawLibrary
                : null;

            return DrawFromLibrary(library, count);
        }

        /// <summary>
        /// 从指定的CardLibrary中抽取卡牌。这是核心抽卡逻辑。
        /// [伪代码]
        /// 1. 验证参数：count > 0，library非空且contains cards
        /// 2. 检查手牌是否已满 → 满则返回空列表
        /// 3. 循环count次：
        ///    - 检查手牌是否达到上限 → 达到则break
        ///    - PickRandomCard(library) 随机选择一张卡
        ///    - CreateCardInstance(data) 创建CardInstance实例
        ///    - 添加到_hand和drawn列表
        /// 4. UpdateDebugInfo() 更新Inspector信息
        /// 5. 返回drawn列表（实际抽到的卡牌）
        ///
        /// 失败情况处理：
        /// - library为null → 警告，返回空列表（抽卡失败）
        /// - library.cards为空 → 警告，返回空列表
        /// - 手牌满 → 日志，返回空列表
        /// - 单次创建卡失败 → 日志，break循环（提前停止）
        /// </summary>
        /// <param name="library">卡牌库</param>
        /// <param name="count">要抽取的卡牌数</param>
        /// <returns>成功抽取的卡牌列表</returns>
        public List<CardInstance> DrawFromLibrary(CardLibrary library, int count)
        {
            var drawn = new List<CardInstance>();
            if (count <= 0)
            {
                return drawn;
            }

            if (library == null)
            {
                Debug.LogWarning("[DeckManager] Cannot draw: library is null.");
                return drawn;
            }

            if (library.cards == null || library.cards.Count == 0)
            {
                Debug.LogWarning($"[DeckManager] Library '{library.DisplayName}' is empty.");
                return drawn;
            }

            if (_hand.Count >= _maxHandSize)
            {
                Debug.Log($"[DeckManager] Hand is full ({_hand.Count}/{_maxHandSize}).");
                return drawn;
            }

            // 尝试抽取count张卡牌（受手牌上限限制）
            for (int i = 0; i < count; i++)
            {
                if (_hand.Count >= _maxHandSize)
                {
                    Debug.Log($"[DeckManager] Hand reached cap ({_hand.Count}/{_maxHandSize}).");
                    break;
                }

                var data = PickRandomCard(library);  // 从库中随机选卡
                if (data == null)
                {
                    Debug.LogWarning($"[DeckManager] Library '{library.DisplayName}' produced a null card.");
                    break;
                }

                var card = CreateCardInstance(data);  // 创建卡牌实例
                _hand.Add(card);
                drawn.Add(card);
            }

            UpdateDebugInfo();
            Debug.Log(
                $"[DeckManager] Drew {drawn.Count} card(s) from library '{library.DisplayName}'. Hand={_hand.Count}, Discard={_discardPile.Count}.");
            return drawn;
        }

        /// <summary>
        /// 从手牌移除卡牌。用于卡牌被使用或被效果移除时调用。
        /// </summary>
        /// <param name="card">要移除的卡牌实例</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveFromHand(CardInstance card)
        {
            if (!_hand.Remove(card))
            {
                Debug.LogWarning($"[DeckManager] Card not found in hand: {card}");
                return false;
            }

            UpdateDebugInfo();
            return true;
        }

        /// <summary>
        /// 生成卡牌并添加到手牌或弃卡池。用于卡牌效果生成新卡或其他需要动态生成卡牌的场景。
        /// [伪代码]
        /// 1. 验证data非空
        /// 2. CreateCardInstance(data) 创建卡牌实例
        /// 3. 检查手牌是否满：
        ///    - 满：添加到_discardPile，日志记录
        ///    - 不满：添加到_hand
        /// 4. UpdateDebugInfo()
        /// </summary>
        /// <param name="data">要生成的卡牌数据</param>
        public CardInstance AddCardToHand(CardData data)
        {
            if (data == null)
            {
                return null;
            }

            var card = CreateCardInstance(data);

            // 如果手牌已满，生成的卡进入弃卡池
            if (_hand.Count >= _maxHandSize)
            {
                _discardPile.Add(card);
                Debug.Log($"[DeckManager] Hand full, generated card sent to discard: {data.cardName}");
            }
            else
            {
                _hand.Add(card);
                Debug.Log($"[DeckManager] Added generated card to hand: {data.cardName}");
            }

            UpdateDebugInfo();
            return card;
        }

        /// <summary>
        /// 处理手牌中的等待卡（waitTurns > 0）。在每个回合开始或结束时调用。
        /// [伪代码]
        /// 1. 反向遍历_hand（避免删除时破坏索引）
        /// 2. 对于每张卡：
        ///    - 如果卡的waitTurns <= 0，跳过（不需要等待）
        ///    - 否则，CurrentWait--（倒数）
        ///    - 如果CurrentWait <= 0:
        ///      └─ 从手牌移除
        /// 3. 返回本次从手牌清除的等待卡牌数
        ///
        /// 用途：
        /// - 处理需要等待N回合后才能使用的卡（如"禁用卡"）
        /// - 等待清零时仅从手牌清除，不触发销毁效果或弃牌逻辑
        /// </summary>
        /// <returns>本次从手牌清除的等待卡牌数量</returns>
        public int ResolveHandWaitAndDiscardExpired()
        {
            int removedFromHand = 0;

            // 反向遍历以安全移除
            for (int i = _hand.Count - 1; i >= 0; i--)
            {
                var card = _hand[i];

                // 如果卡不需要等待，跳过
                if (card.Data.waitTurns <= 0)
                {
                    continue;
                }

                // 倒数等待回合
                card.CurrentWait--;

                // 如果等待完成
                if (card.CurrentWait <= 0)
                {
                    _hand.RemoveAt(i);
                    removedFromHand++;
                }
            }

            if (removedFromHand > 0)
            {
                Debug.Log($"[DeckManager] Removed {removedFromHand} expired waiting card(s) from hand.");
            }

            UpdateDebugInfo();
            return removedFromHand;
        }

        /// <summary>
        /// 更新Inspector显示的调试信息。当手牌或弃卡池变化时调用。
        /// </summary>
        private void UpdateDebugInfo()
        {
            _drawPileCount = 0;                    // 抽卡池信息暂不实现（未来可支持）
            _handCount = _hand.Count;
            _discardPileCount = _discardPile.Count;
        }

        /// <summary>
        /// 工厂方法：从CardData创建CardInstance实例。
        /// 集中化创建逻辑，便于扩展（如卡牌初始化、事件挂钩等）。
        /// </summary>
        /// <param name="data">卡牌数据</param>
        /// <returns>新创建的卡牌实例</returns>
        private static CardInstance CreateCardInstance(CardData data)
        {
            return new CardInstance(data);
        }

        /// <summary>
        /// 从CardLibrary中随机选取一张卡牌数据。
        /// 支持卡牌稀有度权重分布（由CardLibrary内部实现）。
        /// </summary>
        /// <param name="library">卡牌库</param>
        /// <returns>随机选中的卡牌数据，或null如果库为空</returns>
        private static CardData PickRandomCard(CardLibrary library)
        {
            if (library == null || library.cards == null || library.cards.Count == 0)
            {
                return null;
            }

            // 简单均匀随机（未来可扩展为加权分布）
            int index = Random.Range(0, library.cards.Count);
            return library.cards[index];
        }
    }
}

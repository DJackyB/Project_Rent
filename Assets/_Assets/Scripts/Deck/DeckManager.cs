using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Core;
using UnityEngine;

namespace BaoZuPo.Deck
{
    /// <summary>
    /// 卡牌管理器：负责抽卡池、手牌、弃卡池的生命周期管理。
    ///
    /// 职责：
    /// 1. 手牌管理：维护玩家手中的卡牌列表，限制手牌大小（默认7张）
    /// 2. 抽卡池管理：由 InitializeDeck 根据 CardLibrary 的 entries/quantity 初始化
    /// 3. 弃卡池管理：打出的即发卡进入弃卡池；抽卡池耗尽时将弃卡池洗牌后补充
    /// 4. 卡牌生成：DrawFromLibrary 用于奖励池等特殊抽取（不走抽卡池，动态生成实例）
    /// 5. 过期卡管理：处理带等待回合数的卡牌（waitTurns > 0）
    ///
    /// 核心流程：
    /// [初始化] → Initialize(maxHandSize) → InitializeDeck(library)
    /// [每回合抽卡] → Draw(count)
    ///   └─ 从 _drawPile 尾部取牌 → 不足则 ShuffleDiscardIntoDraw() → 加入 _hand
    /// [使用即发卡] → RemoveFromHand() → SendToDiscard()
    /// [使用上场卡] → RemoveFromHand()（不进弃卡池，牌在场上）
    /// [过期卡] → ResolveHandWaitAndDiscardExpired() 每回合调用
    /// [特殊抽取] → DrawFromLibrary(library, count) 动态生成，不走抽卡池
    /// </summary>
    public class DeckManager : Core.Singleton<DeckManager>
    {
        [Header("Debug")]
        [SerializeField] private int _drawPileCount;
        [SerializeField] private int _handCount;
        [SerializeField] private int _discardPileCount;

        private readonly List<CardInstance> _drawPile = new();
        private readonly List<CardInstance> _hand = new();
        private readonly List<CardInstance> _discardPile = new();

        private int _maxHandSize = 7;

        public IReadOnlyList<CardInstance> Hand => _hand;
        public IReadOnlyList<CardInstance> DrawPile => _drawPile;
        public IReadOnlyList<CardInstance> DiscardPile => _discardPile;
        public int DrawPileCount => _drawPile.Count;
        public int HandCount => _hand.Count;
        public int DiscardPileCount => _discardPile.Count;
        public int MaxHandSize => _maxHandSize;

        /// <summary>
        /// 初始化卡牌系统。清空所有牌堆，设置手牌上限。
        /// 调用后需再调用 InitializeDeck 来填充抽卡池。
        /// </summary>
        public void Initialize(int maxHandSize = 7)
        {
            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();

            _maxHandSize = Mathf.Max(1, maxHandSize);
            UpdateDebugInfo();
            Debug.Log($"[DeckManager] Initialized. Hand size cap: {_maxHandSize}.");
        }

        /// <summary>
        /// 根据 CardLibrary 的 entries 初始化抽卡池并洗牌。
        /// 每张牌按 entry.quantity 决定放入几份实例。
        /// </summary>
        public void InitializeDeck(CardLibrary library)
        {
            if (library == null)
            {
                Debug.LogError("[DeckManager] InitializeDeck: library is null.");
                return;
            }

            _drawPile.Clear();

            foreach (var entry in library.entries)
            {
                if (entry == null || entry.card == null)
                {
                    continue;
                }

                for (int i = 0; i < entry.quantity; i++)
                {
                    _drawPile.Add(CreateCardInstance(entry.card));
                }
            }

            Shuffle(_drawPile);
            UpdateDebugInfo();
            Debug.Log($"[DeckManager] Draw pile initialized with {_drawPile.Count} cards from '{library.DisplayName}'.");
        }

        /// <summary>
        /// 从抽卡池抽取指定数量的牌加入手牌。
        /// 抽卡池不足时自动将弃卡池洗牌补充。
        /// </summary>
        public List<CardInstance> Draw(int count)
        {
            var drawn = new List<CardInstance>();

            if (count <= 0)
            {
                return drawn;
            }

            for (int i = 0; i < count; i++)
            {
                if (_hand.Count >= _maxHandSize)
                {
                    Debug.Log($"[DeckManager] Hand reached cap ({_hand.Count}/{_maxHandSize}).");
                    break;
                }

                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0)
                    {
                        Debug.LogWarning("[DeckManager] Draw pile and discard pile are both empty.");
                        break;
                    }

                    ShuffleDiscardIntoDraw();
                }

                var card = _drawPile[_drawPile.Count - 1];
                _drawPile.RemoveAt(_drawPile.Count - 1);
                _hand.Add(card);
                drawn.Add(card);
            }

            UpdateDebugInfo();
            Debug.Log($"[DeckManager] Drew {drawn.Count} card(s). Draw={_drawPile.Count}, Discard={_discardPile.Count}, Hand={_hand.Count}.");
            return drawn;
        }

        /// <summary>
        /// 从指定的 CardLibrary 中随机抽取卡牌（动态生成实例，不走抽卡池）。
        /// 用于奖励池、事件卡等特殊抽取场景。quantity 忽略，从卡牌列表均匀随机。
        /// </summary>
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

            if (library.entries == null || library.entries.Count == 0)
            {
                Debug.LogWarning($"[DeckManager] Library '{library.DisplayName}' has no entries.");
                return drawn;
            }

            if (_hand.Count >= _maxHandSize)
            {
                Debug.Log($"[DeckManager] Hand is full ({_hand.Count}/{_maxHandSize}).");
                return drawn;
            }

            for (int i = 0; i < count; i++)
            {
                if (_hand.Count >= _maxHandSize)
                {
                    Debug.Log($"[DeckManager] Hand reached cap ({_hand.Count}/{_maxHandSize}).");
                    break;
                }

                var data = PickRandomCard(library);
                if (data == null)
                {
                    Debug.LogWarning($"[DeckManager] Library '{library.DisplayName}' produced a null card.");
                    break;
                }

                var card = CreateCardInstance(data);
                _hand.Add(card);
                drawn.Add(card);
            }

            UpdateDebugInfo();
            Debug.Log($"[DeckManager] Drew {drawn.Count} card(s) from library '{library.DisplayName}'. Hand={_hand.Count}.");
            return drawn;
        }

        /// <summary>
        /// 从手牌移除卡牌。卡牌被使用或被效果移除时调用。
        /// </summary>
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
        /// 将卡牌送入弃卡池。即发卡打出后调用，参与下一轮洗牌循环。
        /// </summary>
        public void SendToDiscard(CardInstance card)
        {
            _discardPile.Add(card);
            UpdateDebugInfo();
            Debug.Log($"[DeckManager] Card sent to discard: {card}");
        }

        /// <summary>
        /// 生成卡牌并添加到手牌（手牌满则进弃卡池）。用于卡牌效果生成新卡。
        /// </summary>
        public CardInstance AddCardToHand(CardData data)
        {
            if (data == null)
            {
                return null;
            }

            var card = CreateCardInstance(data);

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
        /// 处理手牌中的等待卡（waitTurns > 0）。在每个回合开始时调用。
        /// 等待倒计时归零的卡从手牌清除，不触发销毁效果，不进弃卡池。
        /// </summary>
        public int ResolveHandWaitAndDiscardExpired()
        {
            int removedFromHand = 0;

            for (int i = _hand.Count - 1; i >= 0; i--)
            {
                var card = _hand[i];

                if (card.Data.waitTurns <= 0)
                {
                    continue;
                }

                card.CurrentWait--;

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

        // ── 私有方法 ──────────────────────────────────────────────────────────

        public void ShuffleDiscardIntoDraw()
        {
            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
            Debug.Log($"[DeckManager] Shuffled discard into draw pile. Draw pile: {_drawPile.Count} cards.");
        }

        private static void Shuffle(List<CardInstance> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static CardInstance CreateCardInstance(CardData data)
        {
            return new CardInstance(data);
        }

        /// <summary>
        /// 从 CardLibrary 的 entries 中均匀随机选取一张牌（quantity 忽略）。
        /// </summary>
        private static CardData PickRandomCard(CardLibrary library)
        {
            if (library == null || library.entries == null || library.entries.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, library.entries.Count);
            return library.entries[index].card;
        }

        private void UpdateDebugInfo()
        {
            _drawPileCount = _drawPile.Count;
            _handCount = _hand.Count;
            _discardPileCount = _discardPile.Count;
        }
    }
}

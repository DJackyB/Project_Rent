using System.Collections.Generic;
using UnityEngine;
using BaoZuPo.Card;

namespace BaoZuPo.Deck
{
    /// <summary>
    /// 牌组管理器
    /// 管理抽牌堆、手牌、弃牌堆的流转。
    /// </summary>
    public class DeckManager : Core.Singleton<DeckManager>
    {
        [Header("调试信息")]
        [SerializeField] private int _drawPileCount;
        [SerializeField] private int _handCount;
        [SerializeField] private int _discardPileCount;

        private readonly List<CardInstance> _drawPile = new();      // 抽牌堆
        private readonly List<CardInstance> _hand = new();          // 手牌
        private readonly List<CardInstance> _discardPile = new();   // 弃牌堆

        private int _maxHandSize = 7;

        public IReadOnlyList<CardInstance> Hand => _hand;
        public int DrawPileCount => _drawPile.Count;
        public int HandCount => _hand.Count;
        public int DiscardPileCount => _discardPile.Count;
        public int MaxHandSize => _maxHandSize;

        /// <summary>
        /// 初始化牌组（用所有卡牌数据构建抽牌堆）
        /// </summary>
        public void Initialize(IEnumerable<CardData> allCards, int maxHandSize = 7)
        {
            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();

            _maxHandSize = Mathf.Max(1, maxHandSize);

            foreach (var data in allCards)
            {
                _drawPile.Add(new CardInstance(data));
            }

            Shuffle(_drawPile);
            UpdateDebugInfo();
            Debug.Log($"[DeckManager] 初始化完成，抽牌堆 {_drawPile.Count} 张，手牌上限 {_maxHandSize}");
        }

        /// <summary>
        /// 抽牌
        /// </summary>
        public List<CardInstance> Draw(int count)
        {
            var drawn = new List<CardInstance>();
            if (count <= 0) return drawn;

            if (_hand.Count >= _maxHandSize)
            {
                Debug.Log($"[DeckManager] 手牌已满（{_hand.Count}/{_maxHandSize}），本次抽卡跳过");
                return drawn;
            }

            for (int i = 0; i < count; i++)
            {
                if (_hand.Count >= _maxHandSize)
                {
                    Debug.Log($"[DeckManager] 手牌达到上限（{_hand.Count}/{_maxHandSize}），停止本次后续抽卡");
                    break;
                }

                if (_drawPile.Count == 0)
                {
                    RefillDrawPileFromDiscard();
                    if (_drawPile.Count == 0)
                    {
                        Debug.LogWarning("[DeckManager] 抽牌堆与弃牌堆都为空，无法继续抽牌");
                        break;
                    }
                }

                var card = _drawPile[0];
                _drawPile.RemoveAt(0);

                // 抽到手牌时重置等待回合
                card.CurrentWait = card.Data.waitTurns;

                _hand.Add(card);
                drawn.Add(card);
            }

            UpdateDebugInfo();
            Debug.Log($"[DeckManager] 抽了 {drawn.Count} 张牌，手牌 {_hand.Count} 张，抽牌堆 {_drawPile.Count} 张，弃牌堆 {_discardPile.Count} 张");
            return drawn;
        }

        /// <summary>
        /// 从手牌中打出一张牌（直接移除）
        /// </summary>
        public bool RemoveFromHand(CardInstance card)
        {
            if (!_hand.Remove(card))
            {
                Debug.LogWarning($"[DeckManager] 手牌中没有这张牌: {card}");
                return false;
            }

            UpdateDebugInfo();
            return true;
        }

        /// <summary>
        /// 回合结束时处理手牌等待倒计时：
        /// 1. waitTurns > 0 的手牌 CurrentWait -1
        /// 2. CurrentWait <= 0 的牌加入弃牌堆
        /// </summary>
        public int ResolveHandWaitAndDiscardExpired()
        {
            int movedToDiscard = 0;

            for (int i = _hand.Count - 1; i >= 0; i--)
            {
                var card = _hand[i];

                // waitTurns = 0 表示无等待机制
                if (card.Data.waitTurns <= 0) continue;

                card.CurrentWait--;
                if (card.CurrentWait <= 0)
                {
                    _hand.RemoveAt(i);
                    _discardPile.Add(card);
                    movedToDiscard++;
                }
            }

            if (movedToDiscard > 0)
            {
                Debug.Log($"[DeckManager] 回合结束移入弃牌堆 {movedToDiscard} 张（手牌等待归零）");
            }

            UpdateDebugInfo();
            return movedToDiscard;
        }

        /// <summary>
        /// 将弃牌堆洗回抽牌堆
        /// </summary>
        private void RefillDrawPileFromDiscard()
        {
            if (_discardPile.Count == 0) return;

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);

            Debug.Log($"[DeckManager] 弃牌堆回洗到抽牌堆，当前抽牌堆 {_drawPile.Count} 张");
            UpdateDebugInfo();
        }

        /// <summary>
        /// 洗牌（Fisher-Yates）
        /// </summary>
        private void Shuffle(List<CardInstance> pile)
        {
            for (int i = pile.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pile[i], pile[j]) = (pile[j], pile[i]);
            }
        }

        private void UpdateDebugInfo()
        {
            _drawPileCount = _drawPile.Count;
            _handCount = _hand.Count;
            _discardPileCount = _discardPile.Count;
        }
    }
}

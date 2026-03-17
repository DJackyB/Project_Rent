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

        private List<CardInstance> _drawPile = new();    // 抽牌堆
        private List<CardInstance> _hand = new();         // 手牌

        public IReadOnlyList<CardInstance> Hand => _hand;
        public int DrawPileCount => _drawPile.Count;
        public int HandCount => _hand.Count;

        /// <summary>
        /// 初始化牌组（用所有卡牌数据构建抽牌堆）
        /// </summary>
        public void Initialize(IEnumerable<CardData> allCards)
        {
            _drawPile.Clear();
            _hand.Clear();

            foreach (var data in allCards)
            {
                _drawPile.Add(new CardInstance(data));
            }

            Shuffle();
            UpdateDebugInfo();
            Debug.Log($"[DeckManager] 初始化完成，抽牌堆 {_drawPile.Count} 张");
        }

        /// <summary>
        /// 抽牌
        /// </summary>
        public List<CardInstance> Draw(int count)
        {
            var drawn = new List<CardInstance>();

            for (int i = 0; i < count; i++)
            {
                if (_drawPile.Count == 0)
                {
                    Debug.LogWarning("[DeckManager] 抽牌堆已空，无法继续抽牌");
                    break;
                }

                var card = _drawPile[0];
                _drawPile.RemoveAt(0);
                _hand.Add(card);
                drawn.Add(card);
            }

            UpdateDebugInfo();
            Debug.Log($"[DeckManager] 抽了 {drawn.Count} 张牌，手牌 {_hand.Count} 张");
            return drawn;
        }

        /// <summary>
        /// 从手牌中打出一张牌（直接销毁/移除）
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
        /// 洗牌（Fisher-Yates）
        /// </summary>
        private void Shuffle()
        {
            for (int i = _drawPile.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]);
            }
        }

        private void UpdateDebugInfo()
        {
            _drawPileCount = _drawPile.Count;
            _handCount = _hand.Count;
        }
    }
}

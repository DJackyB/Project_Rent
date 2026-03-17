using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌数据库
    /// 加载并管理所有 CardData 资产，提供按 ID 查询功能。
    /// </summary>
    public static class CardDatabase
    {
        private static readonly Dictionary<int, CardData> _cards = new();
        private static bool _isLoaded = false;

        /// <summary>
        /// 从 Resources 文件夹或指定路径加载所有 CardData
        /// </summary>
        public static void LoadAll(string resourcePath = "Cards")
        {
            _cards.Clear();
            var allCards = Resources.LoadAll<CardData>(resourcePath);

            foreach (var card in allCards)
            {
                if (_cards.ContainsKey(card.cardId))
                {
                    Debug.LogWarning($"[CardDatabase] 重复的卡牌ID: {card.cardId} ({card.cardName})，已跳过");
                    continue;
                }

                _cards[card.cardId] = card;
            }

            _isLoaded = true;
            Debug.Log($"[CardDatabase] 已加载 {_cards.Count} 张卡牌数据");
        }

        /// <summary>
        /// 手动注册单张卡牌（用于导表后直接注册，不走 Resources）
        /// </summary>
        public static void Register(CardData data)
        {
            if (data == null) return;
            _cards[data.cardId] = data;
        }

        /// <summary>
        /// 按 ID 获取卡牌数据
        /// </summary>
        public static CardData GetById(int cardId)
        {
            if (!_isLoaded)
            {
                Debug.LogWarning("[CardDatabase] 数据库未加载，正在自动加载...");
                LoadAll();
            }

            _cards.TryGetValue(cardId, out var data);
            return data;
        }

        /// <summary>
        /// 获取所有已加载的卡牌数据
        /// </summary>
        public static IReadOnlyDictionary<int, CardData> GetAll()
        {
            if (!_isLoaded) LoadAll();
            return _cards;
        }

        /// <summary>
        /// 清除数据库
        /// </summary>
        public static void Clear()
        {
            _cards.Clear();
            _isLoaded = false;
        }
    }
}

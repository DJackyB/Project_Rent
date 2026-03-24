using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌数据库 用于加载和查询所有 CardData 资源
    /// </summary>
    public static class CardDatabase
    {
        private static readonly Dictionary<int, CardData> _cards = new();
        private static bool _isLoaded = false;

        /// <summary>
        /// 从 Resources 路径加载全部 CardData
        /// </summary>
        public static void LoadAll(string resourcePath = "Cards")
        {
            _cards.Clear();
            var allCards = Resources.LoadAll<CardData>(resourcePath);

            foreach (var card in allCards)
            {
                if (_cards.ContainsKey(card.cardId))
                {
                    Debug.LogWarning($"[CardDatabase] \u68c0\u6d4b\u5230\u91cd\u590d\u5361\u724c ID {card.cardId} {card.cardName} \u5df2\u8df3\u8fc7");
                    continue;
                }

                _cards[card.cardId] = card;
            }

            _isLoaded = true;
            Debug.Log($"[CardDatabase] \u5df2\u52a0\u8f7d {_cards.Count} \u5f20\u5361\u724c");
        }

        /// <summary>
        /// 手动注册单张卡牌 通常用于导入后补登记
        /// </summary>
        public static void Register(CardData data)
        {
            if (data == null) return;
            _cards[data.cardId] = data;
        }

        /// <summary>
        /// 根据 ID 获取卡牌数据
        /// </summary>
        public static CardData GetById(int cardId)
        {
            if (!_isLoaded)
            {
                Debug.LogWarning("[CardDatabase] \u5361\u724c\u6570\u636e\u5e93\u5c1a\u672a\u52a0\u8f7d \u6b63\u5728\u81ea\u52a8\u52a0\u8f7d");
                LoadAll();
            }

            _cards.TryGetValue(cardId, out var data);
            return data;
        }

        /// <summary>
        /// 获取当前已加载的全部卡牌
        /// </summary>
        public static IReadOnlyDictionary<int, CardData> GetAll()
        {
            if (!_isLoaded) LoadAll();
            return _cards;
        }

        /// <summary>
        /// 清空数据库缓存
        /// </summary>
        public static void Clear()
        {
            _cards.Clear();
            _isLoaded = false;
        }
    }
}

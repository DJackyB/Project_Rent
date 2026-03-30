using System.Collections.Generic;
using System;
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
                if (card == null)
                {
                    throw new InvalidOperationException("[CardDatabase] Encountered a null CardData asset while loading.");
                }

                if (_cards.ContainsKey(card.cardId))
                {
                    CardData existing = _cards[card.cardId];
                    throw new InvalidOperationException(
                        $"[CardDatabase] Duplicate cardId detected: {card.cardId}. Existing={existing.cardName}, Incoming={card.cardName}");
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
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "[CardDatabase] Cannot register a null CardData.");
            }

            if (_cards.TryGetValue(data.cardId, out var existing) && existing != data)
            {
                throw new InvalidOperationException(
                    $"[CardDatabase] Duplicate cardId detected during Register: {data.cardId}. Existing={existing.cardName}, Incoming={data.cardName}");
            }

            _cards[data.cardId] = data;
            _isLoaded = true;
        }

        /// <summary>
        /// 根据 ID 获取卡牌数据
        /// </summary>
        public static CardData GetById(int cardId)
        {
            if (!_isLoaded)
            {
                throw new InvalidOperationException("[CardDatabase] Accessed before LoadAll().");
            }

            _cards.TryGetValue(cardId, out var data);
            return data;
        }

        /// <summary>
        /// 获取当前已加载的全部卡牌
        /// </summary>
        public static IReadOnlyDictionary<int, CardData> GetAll()
        {
            if (!_isLoaded)
            {
                throw new InvalidOperationException("[CardDatabase] Accessed before LoadAll().");
            }

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

        public static bool IsLoaded => _isLoaded;
    }
}

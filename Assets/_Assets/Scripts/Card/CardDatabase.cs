using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌数据库。
    ///
    /// 从 Resources/Cards/ 目录加载所有 CardData 资源，并提供 ID 索引访问。
    /// 必须在游戏启动时调用 LoadAll() 初始化。
    ///
    /// 特点：
    /// - 单例模式（静态类），生命周期贯穿游戏运行
    /// - 按卡牌 ID（int）索引存储和查询
    /// - 检测并报错重复 ID，防止配置错误
    /// - 配置错误直接抛异常，不做静默 fallback
    /// </summary>
    public static class CardDatabase
    {
        /// <summary>卡牌 ID 到 CardData 的映射表。</summary>
        private static readonly Dictionary<int, CardData> _cards = new();

        /// <summary>是否已加载过数据。用于检测未初始化访问。</summary>
        private static bool _isLoaded;

        /// <summary>
        /// 从 Resources 目录加载所有卡牌数据。
        ///
        /// 应在游戏启动早期（如 GameBootstrap）调用一次。
        /// 加载失败时会直接抛异常。
        ///
        /// 参数：
        /// - resourcePath：资源路径（相对于 Assets/Resources/）。默认 "Cards"
        ///
        /// 异常：
        /// - null 资源：InvalidOperationException
        /// - 重复 ID：InvalidOperationException（包含冲突卡牌的名称）
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
            Debug.Log($"[CardDatabase] Loaded {_cards.Count} cards.");
        }

        /// <summary>
        /// 手动注册一张卡牌（用于运行时或测试）。
        ///
        /// 检测并报错重复 ID。
        ///
        /// 异常：
        /// - null 数据：ArgumentNullException
        /// - 重复 ID：InvalidOperationException
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
        }

        /// <summary>
        /// 按 ID 查询卡牌数据。
        ///
        /// 返回：CardData 实例，或 null（如果 ID 不存在）。
        ///
        /// 异常：
        /// - 未调用 LoadAll()：InvalidOperationException
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
        /// 返回所有已加载的卡牌数据（只读）。
        ///
        /// 返回：ID 到 CardData 的不可修改映射。
        ///
        /// 异常：
        /// - 未调用 LoadAll()：InvalidOperationException
        /// </summary>
        public static IReadOnlyDictionary<int, CardData> GetAll()
        {
            if (!_isLoaded)
            {
                throw new InvalidOperationException("[CardDatabase] Accessed before LoadAll().");
            }

            return _cards;
        }

        /// <summary>清空所有数据，重置加载状态。通常用于测试清理。</summary>
        public static void Clear()
        {
            _cards.Clear();
            _isLoaded = false;
        }
    }
}

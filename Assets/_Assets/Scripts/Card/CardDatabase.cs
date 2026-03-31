using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
    public static class CardDatabase
    {
        private static readonly Dictionary<int, CardData> _cards = new();
        private static bool _isLoaded;

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

        public static CardData GetById(int cardId)
        {
            if (!_isLoaded)
            {
                throw new InvalidOperationException("[CardDatabase] Accessed before LoadAll().");
            }

            _cards.TryGetValue(cardId, out var data);
            return data;
        }

        public static IReadOnlyDictionary<int, CardData> GetAll()
        {
            if (!_isLoaded)
            {
                throw new InvalidOperationException("[CardDatabase] Accessed before LoadAll().");
            }

            return _cards;
        }

        public static void Clear()
        {
            _cards.Clear();
            _isLoaded = false;
        }
    }
}

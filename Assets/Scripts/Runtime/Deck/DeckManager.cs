using System;
using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Core;
using UnityEngine;

namespace BaoZuPo.Deck
{
    /// <summary>
    /// 负责抽牌堆、手牌和弃牌堆的生命周期管理。
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

        public void Initialize(int maxHandSize = 7)
        {
            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();

            _maxHandSize = Mathf.Max(1, maxHandSize);
            UpdateDebugInfo();
            Debug.Log($"[DeckManager] Initialized. Hand size cap: {_maxHandSize}.");
        }

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

        public bool ContainsInHand(CardInstance card)
        {
            return card != null && _hand.Contains(card);
        }

        public void SendToDiscard(CardInstance card)
        {
            _discardPile.Add(card);
            UpdateDebugInfo();
            Debug.Log($"[DeckManager] Card sent to discard: {card}");
        }

        public CardInstance AddCardToHand(CardData data)
        {
            return AddCardToHand(data, false, null);
        }

        public CardInstance AddCardToHand(CardData data, bool ignoreHandLimit, Action<CardInstance> configureInstance)
        {
            if (data == null)
            {
                return null;
            }

            var card = CreateCardInstance(data);
            configureInstance?.Invoke(card);

            if (!ignoreHandLimit && _hand.Count >= _maxHandSize)
            {
                _discardPile.Add(card);
                Debug.Log($"[DeckManager] Hand full, generated card sent to discard: {data.cardName}");
            }
            else
            {
                _hand.Add(card);
                Debug.Log(ignoreHandLimit
                    ? $"[DeckManager] Force-added generated card to hand: {data.cardName}"
                    : $"[DeckManager] Added generated card to hand: {data.cardName}");
            }

            UpdateDebugInfo();
            return card;
        }

        public CardInstance ForceAddCardToHand(CardData data, Action<CardInstance> configureInstance = null)
        {
            return AddCardToHand(data, true, configureInstance);
        }

        public CardInstance AddCardToDrawPile(CardData data)
        {
            if (data == null)
            {
                return null;
            }

            var card = CreateCardInstance(data);
            int insertIndex = UnityEngine.Random.Range(0, _drawPile.Count + 1);
            _drawPile.Insert(insertIndex, card);
            UpdateDebugInfo();
            Debug.Log($"[DeckManager] Added generated card to draw pile at random index {insertIndex}: {data.cardName}");
            return card;
        }

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
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static CardInstance CreateCardInstance(CardData data)
        {
            return new CardInstance(data);
        }

        private static CardData PickRandomCard(CardLibrary library)
        {
            if (library == null || library.entries == null || library.entries.Count == 0)
            {
                return null;
            }

            int index = UnityEngine.Random.Range(0, library.entries.Count);
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

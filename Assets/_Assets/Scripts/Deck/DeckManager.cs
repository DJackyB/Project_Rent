using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Save;
using UnityEngine;

namespace BaoZuPo.Deck
{
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
        public int DrawPileCount => _drawPile.Count;
        public int HandCount => _hand.Count;
        public int DiscardPileCount => _discardPile.Count;
        public int MaxHandSize => _maxHandSize;

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
            Debug.Log($"[DeckManager] Initialized deck with {_drawPile.Count} cards. Hand size cap: {_maxHandSize}.");
        }

        public List<CardInstance> Draw(int count)
        {
            var drawn = new List<CardInstance>();
            if (count <= 0)
            {
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

                if (_drawPile.Count == 0)
                {
                    RefillDrawPileFromDiscard();
                    if (_drawPile.Count == 0)
                    {
                        Debug.LogWarning("[DeckManager] Draw pile and discard pile are both empty.");
                        break;
                    }
                }

                var card = _drawPile[0];
                _drawPile.RemoveAt(0);
                card.CurrentWait = card.Data.waitTurns;

                _hand.Add(card);
                drawn.Add(card);
            }

            UpdateDebugInfo();
            Debug.Log($"[DeckManager] Drew {drawn.Count} card(s). Hand={_hand.Count}, Draw={_drawPile.Count}, Discard={_discardPile.Count}.");
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

        public void AddCardToHand(CardData data)
        {
            if (data == null)
            {
                return;
            }

            var card = new CardInstance(data)
            {
                CurrentWait = data.waitTurns
            };

            if (_hand.Count >= _maxHandSize)
            {
                _discardPile.Add(card);
                Debug.Log($"[DeckManager] Hand full, reward card sent to discard: {data.cardName}");
            }
            else
            {
                _hand.Add(card);
                Debug.Log($"[DeckManager] Added reward card to hand: {data.cardName}");
            }

            UpdateDebugInfo();
        }

        public int ResolveHandWaitAndDiscardExpired()
        {
            int movedToDiscard = 0;

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

                    if (card.Data.cardType == CardType.Tenant)
                    {
                        card.MarkDestroyed();
                    }
                    else
                    {
                        _discardPile.Add(card);
                        movedToDiscard++;
                    }
                }
            }

            if (movedToDiscard > 0)
            {
                Debug.Log($"[DeckManager] Moved {movedToDiscard} expired card(s) to discard.");
            }

            UpdateDebugInfo();
            return movedToDiscard;
        }

        private void RefillDrawPileFromDiscard()
        {
            if (_discardPile.Count == 0)
            {
                return;
            }

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);

            Debug.Log($"[DeckManager] Refilled draw pile. Draw={_drawPile.Count}.");
            UpdateDebugInfo();
        }

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

        public DeckSaveState CaptureState()
        {
            var state = new DeckSaveState
            {
                maxHandSize = _maxHandSize
            };

            CapturePile(_drawPile, state.drawPile);
            CapturePile(_hand, state.hand);
            CapturePile(_discardPile, state.discardPile);
            return state;
        }

        public void RestoreState(DeckSaveState state)
        {
            if (state == null)
            {
                throw new System.ArgumentNullException(nameof(state));
            }

            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();
            _maxHandSize = Mathf.Max(1, state.maxHandSize);

            RestorePile(state.drawPile, _drawPile, "drawPile");
            RestorePile(state.hand, _hand, "hand");
            RestorePile(state.discardPile, _discardPile, "discardPile");
            UpdateDebugInfo();
        }

        private static void CapturePile(IReadOnlyList<CardInstance> source, List<CardRuntimeState> target)
        {
            if (source == null || target == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                var card = source[i];
                if (card == null || card.IsDestroyed)
                {
                    continue;
                }

                target.Add(card.CaptureRuntimeState());
            }
        }

        private static void RestorePile(IReadOnlyList<CardRuntimeState> source, List<CardInstance> target, string label)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (!CardInstance.TryCreateFromRuntimeState(source[i], out var card, out var error))
                {
                    throw new System.InvalidOperationException($"[DeckManager] Failed to restore {label}[{i}]. {error}");
                }

                target.Add(card);
            }
        }
    }
}

using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Core;
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

        private readonly List<CardInstance> _hand = new();
        private readonly List<CardInstance> _discardPile = new();

        private int _maxHandSize = 7;

        public IReadOnlyList<CardInstance> Hand => _hand;
        public int DrawPileCount => _drawPileCount;
        public int HandCount => _hand.Count;
        public int DiscardPileCount => _discardPile.Count;
        public int MaxHandSize => _maxHandSize;

        public void Initialize(int maxHandSize = 7)
        {
            _hand.Clear();
            _discardPile.Clear();

            _maxHandSize = Mathf.Max(1, maxHandSize);
            UpdateDebugInfo();
            Debug.Log($"[DeckManager] Initialized card zones. Hand size cap: {_maxHandSize}.");
        }

        public List<CardInstance> Draw(int count)
        {
            CardLibrary library = GameManager.Instance != null && GameManager.Instance.gameConfig != null
                ? GameManager.Instance.gameConfig.normalTurnDrawLibrary
                : null;

            return DrawFromLibrary(library, count);
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

            if (library.cards == null || library.cards.Count == 0)
            {
                Debug.LogWarning($"[DeckManager] Library '{library.DisplayName}' is empty.");
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
            Debug.Log(
                $"[DeckManager] Drew {drawn.Count} card(s) from library '{library.DisplayName}'. Hand={_hand.Count}, Discard={_discardPile.Count}.");
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

        private void UpdateDebugInfo()
        {
            _drawPileCount = 0;
            _handCount = _hand.Count;
            _discardPileCount = _discardPile.Count;
        }

        private static CardInstance CreateCardInstance(CardData data)
        {
            return new CardInstance(data);
        }

        private static CardData PickRandomCard(CardLibrary library)
        {
            if (library == null || library.cards == null || library.cards.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, library.cards.Count);
            return library.cards[index];
        }

        public DeckSaveState CaptureState()
        {
            var state = new DeckSaveState
            {
                maxHandSize = _maxHandSize
            };

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

            _hand.Clear();
            _discardPile.Clear();
            _maxHandSize = Mathf.Max(1, state.maxHandSize);

            if (state.drawPile != null && state.drawPile.Count > 0)
            {
                Debug.LogWarning(
                    $"[DeckManager] Ignoring legacy drawPile state with {state.drawPile.Count} card(s); runtime draw now comes from configured card libraries.");
            }

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

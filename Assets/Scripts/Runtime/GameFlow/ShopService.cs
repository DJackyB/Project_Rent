using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Economy;
using BaoZuPo.UI;
using Cysharp.Threading.Tasks;
using Martian.EventBus;
using UnityEngine;

namespace BaoZuPo.GameFlow
{
    public sealed class ShopService : IShopService
    {
        private CardData[] _options = Array.Empty<CardData>();
        private bool[] _purchased = Array.Empty<bool>();
        private bool _isOpen;
        private bool _openedThisTurn;
        private bool _closedThisTurn;

        public bool IsOpen => _isOpen;
        public bool OpenedThisTurn => _openedThisTurn;
        public bool ClosedThisTurn => _closedThisTurn;

        public void Open()
        {
            if (_isOpen || _openedThisTurn || _closedThisTurn)
            {
                Debug.LogWarning("[ShopService] Shop cannot be opened: already opened or closed this turn.");
                return;
            }

            var config = GameManager.Instance?.gameConfig;
            if (config == null)
                throw new InvalidOperationException("[ShopService] GameConfig is missing.");

            if (config.shopLibrary == null)
                throw new InvalidOperationException("[ShopService] GameConfig.shopLibrary is not configured.");

            if (UIManager.Instance == null)
                throw new InvalidOperationException("[ShopService] UIManager is required for shop.");

            var sourceCards = config.shopLibrary.entries != null
                ? config.shopLibrary.entries.Select(e => e?.card).Where(c => c != null).ToList()
                : new List<CardData>();

            if (sourceCards.Count == 0)
            {
                Debug.LogWarning("[ShopService] No shop cards available.");
                return;
            }

            int desiredCount = Mathf.Clamp(config.shopOfferCount, 1, GameConfig.MaxShopOfferCount);
            _options = BuildUniqueOptions(sourceCards, desiredCount);

            if (_options.Length == 0)
            {
                Debug.LogWarning("[ShopService] No unique shop cards available.");
                return;
            }

            if (_options.Length < desiredCount)
                Debug.LogWarning($"[ShopService] Only {_options.Length} unique shop card(s) available.");

            _purchased = new bool[_options.Length];
            _isOpen = true;
            _openedThisTurn = true;

            EventBus.Publish(new GameEvents.ShopOpened { Options = _options });
        }

        public bool TryPurchase(int offerIndex)
        {
            if (!_isOpen || offerIndex < 0 || offerIndex >= _options.Length)
                return false;

            if (_purchased[offerIndex])
                return false;

            var chosen = _options[offerIndex];
            if (chosen == null)
                return false;

            if (chosen.cost > 0 && !MoneyManager.Instance.ReduceMoney(chosen.cost))
                return false;

            Deck.DeckManager.Instance.AddCardToDrawPile(chosen);
            _purchased[offerIndex] = true;
            return true;
        }

        public void Close(int turnNumber)
        {
            if (!_isOpen)
                return;

            _isOpen = false;
            _closedThisTurn = true;
            EventBus.Publish(new GameEvents.ShopClosed { TurnNumber = turnNumber });
        }

        public void ResetForNewTurn()
        {
            _options = Array.Empty<CardData>();
            _purchased = Array.Empty<bool>();
            _isOpen = false;
            _openedThisTurn = false;
            _closedThisTurn = false;
        }

        public async UniTask OpenAndWaitCloseAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            Open();
            if (!_isOpen)
                return;

            var tcs = new UniTaskCompletionSource<bool>();

            void OnClosed(GameEvents.ShopClosed _) => tcs.TrySetResult(true);

            EventBus.Subscribe<GameEvents.ShopClosed>(OnClosed);
            try
            {
                await tcs.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                EventBus.Unsubscribe<GameEvents.ShopClosed>(OnClosed);
            }
        }

        private static CardData[] BuildUniqueOptions(List<CardData> source, int desiredCount)
        {
            var remaining = source.ToList();
            var options = new List<CardData>(desiredCount);

            while (remaining.Count > 0 && options.Count < desiredCount)
            {
                var chosen = remaining[UnityEngine.Random.Range(0, remaining.Count)];
                options.Add(chosen);
                remaining.RemoveAll(c => c != null && (ReferenceEquals(c, chosen) || c.cardId == chosen.cardId));
            }

            return options.ToArray();
        }
    }
}

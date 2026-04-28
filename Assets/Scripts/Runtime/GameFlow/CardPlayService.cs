using System;
using System.Threading;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Deck;
using BaoZuPo.Economy;
using BaoZuPo.Integration.Martian.Feedback;
using BaoZuPo.UI;
using Cysharp.Threading.Tasks;
using Martian.EventBus;
using UnityEngine;

namespace BaoZuPo.GameFlow
{
    public sealed class CardPlayService : ICardPlayService
    {
        public CardPlayTargetKind GetRequiredTargetKind(CardInstance card)
        {
            return CardTargeting.GetRequiredTargetKind(card != null ? card.Data : null);
        }

        public CardPlayValidationResult ValidatePlay(CardInstance card, RoomSlot targetRoom = null)
        {
            CardPlayTargetKind requiredTargetKind = GetRequiredTargetKind(card);
            var turnManager = TurnManager.Instance;

            if (turnManager != null && turnManager.IsGameOver)
            {
                return CardPlayValidationResult.Failure(CardPlayBlockReason.GameOver, requiredTargetKind, targetRoom);
            }

            if (card == null || card.Data == null || card.IsDestroyed)
            {
                return CardPlayValidationResult.Failure(CardPlayBlockReason.InvalidTarget, requiredTargetKind, targetRoom);
            }

            if (turnManager == null || turnManager.CurrentPhase != GamePhase.Action || turnManager.ActionPhaseEnded)
            {
                return CardPlayValidationResult.Failure(CardPlayBlockReason.NotActionPhase, requiredTargetKind, targetRoom);
            }

            var moneyManager = RequireMoneyManager();
            if (!moneyManager.CanAfford(card.Data.cost))
            {
                return CardPlayValidationResult.Failure(CardPlayBlockReason.InsufficientMoney, requiredTargetKind, targetRoom);
            }

            if (requiredTargetKind == CardPlayTargetKind.Room)
            {
                if (targetRoom == null)
                {
                    return CardPlayValidationResult.Failure(CardPlayBlockReason.MissingTarget, requiredTargetKind);
                }

                if (CardTargeting.PersistsInRoom(card.Data))
                {
                    if (card.Data.cardType == CardType.Tenant && !targetRoom.CanPlaceTenant)
                    {
                        return CardPlayValidationResult.Failure(CardPlayBlockReason.TargetFull, requiredTargetKind, targetRoom);
                    }

                    if (card.Data.cardType == CardType.Equipment && !targetRoom.CanPlaceEquipment)
                    {
                        return CardPlayValidationResult.Failure(CardPlayBlockReason.TargetFull, requiredTargetKind, targetRoom);
                    }
                }
            }

            return CardPlayValidationResult.Success(requiredTargetKind, targetRoom);
        }

        public UniTask<CardPlayResult> PlayAsync(
            CardInstance card,
            RoomSlot targetRoom = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(Play(card, targetRoom));
        }

        public CardPlayResult Play(CardInstance card, RoomSlot targetRoom = null)
        {
            var context = RequireGameContext();
            var validation = ValidatePlay(card, targetRoom);
            if (!validation.IsValid)
            {
                return CardPlayResult.Failure(card, validation);
            }

            targetRoom = validation.TargetRoom;
            var previousRoom = context.EffectContext.SelectedRoom;
            context.EffectContext.SelectedRoom = targetRoom;

            try
            {
                if (CardTargeting.PersistsAsContract(card.Data))
                {
                    return ResolveCardAfterPlay(
                        card,
                        context,
                        c => RequireBoardManager().AddContract(c),
                        targetRoom,
                        validation);
                }

                if (CardTargeting.PersistsInRoom(card.Data))
                {
                    if (targetRoom == null || !targetRoom.PlaceCard(card))
                    {
                        Debug.LogWarning($"[CardPlayService] Failed to place {card}");
                        return CardPlayResult.Failure(
                            card,
                            CardPlayValidationResult.Failure(CardPlayBlockReason.TargetFull, validation.RequiredTargetKind, targetRoom));
                    }
                }

                return ResolveCardAfterPlay(card, context, null, targetRoom, validation);
            }
            finally
            {
                context.EffectContext.SelectedRoom = previousRoom;
            }
        }

        private static CardPlayResult ResolveCardAfterPlay(
            CardInstance card,
            GameContext context,
            Action<CardInstance> afterInstant,
            RoomSlot targetRoom,
            CardPlayValidationResult validation)
        {
            var moneyManager = RequireMoneyManager();
            if (!moneyManager.ReduceMoney(card.Data.cost))
            {
                if (targetRoom != null && card.PlacedRoom == targetRoom)
                {
                    targetRoom.RemoveCard(card);
                }

                return CardPlayResult.Failure(
                    card,
                    CardPlayValidationResult.Failure(CardPlayBlockReason.InsufficientMoney, validation.RequiredTargetKind, targetRoom));
            }

            int moneyBeforeInstant = moneyManager.CurrentMoney;
            card.InstantEffect?.Execute(card, context);
            int instantMoneyDelta = moneyManager.CurrentMoney - moneyBeforeInstant;

            afterInstant?.Invoke(card);

            var deckManager = DeckManager.Instance;
            if (deckManager != null && deckManager.ContainsInHand(card))
            {
                deckManager.RemoveFromHand(card);
            }

            if (card.RemoveFromGameAfterPlay)
            {
                card.MarkDestroyed();
            }
            else if (!CardTargeting.PersistsInRoom(card.Data) && !CardTargeting.PersistsAsContract(card.Data))
            {
                RequireDeckManager().SendToDiscard(card);
                UIManager.Instance?.handPanel?.PlayDiscardAnimation(card);
            }

            EventBus.Publish(new GameEvents.CardPlayed { Card = card });
            BaoZuPoFeedbackAdapter.PublishPlayCost(card, targetRoom ?? card.PlacedRoom, card.Data.cost);
            PublishPlaySequence(card, targetRoom ?? card.PlacedRoom, instantMoneyDelta);
            return CardPlayResult.Success(
                card,
                CardPlayValidationResult.Success(validation.RequiredTargetKind, targetRoom ?? card.PlacedRoom));
        }

        private static void PublishPlaySequence(CardInstance card, RoomSlot targetRoom, int moneyDelta)
        {
            if (card == null || moneyDelta == 0)
            {
                return;
            }

            BaoZuPoFeedbackAdapter.PublishInstantMoneyDelta(card, targetRoom, moneyDelta);
        }

        private static GameContext RequireGameContext()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                throw new InvalidOperationException("[CardPlayService] GameManager is required in scene.");
            }

            gameManager.EnsureInitialized();
            if (gameManager.GameContext == null)
            {
                throw new InvalidOperationException("[CardPlayService] GameContext is not initialized.");
            }

            return gameManager.GameContext;
        }

        private static MoneyManager RequireMoneyManager()
        {
            var moneyManager = MoneyManager.Instance;
            if (moneyManager == null)
            {
                throw new InvalidOperationException("[CardPlayService] MoneyManager is required in scene.");
            }

            return moneyManager;
        }

        private static BoardManager RequireBoardManager()
        {
            var boardManager = BoardManager.Instance;
            if (boardManager == null)
            {
                throw new InvalidOperationException("[CardPlayService] BoardManager is required in scene.");
            }

            return boardManager;
        }

        private static DeckManager RequireDeckManager()
        {
            var deckManager = DeckManager.Instance;
            if (deckManager == null)
            {
                throw new InvalidOperationException("[CardPlayService] DeckManager is required in scene.");
            }

            return deckManager;
        }
    }
}

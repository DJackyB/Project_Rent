using System.Collections.Generic;
using System.Linq;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Economy;
using BaoZuPo.Integration.Martian.Feedback;
using Martian.EventBus;
using UnityEngine;
using BaoZuPo.UI;

namespace BaoZuPo.GameFlow
{
    public class TurnManager : Singleton<TurnManager>
    {
        private const float DefaultSettlementMultiplier = 1f;

        [Header("Debug")]
        [SerializeField] private int _currentTurn;
        [SerializeField] private bool _isGameOver;
        [SerializeField] private int _loanPaymentCount;

        public int CurrentTurn => _currentTurn;
        public bool IsGameOver => _isGameOver;
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Prepare;
        public bool ActionPhaseEnded { get; set; }

        public void ExecutePreparePhase()
        {
            if (_isGameOver)
            {
                return;
            }

            _currentTurn++;
            EventBus.Publish(new GameEvents.TurnStarted { TurnNumber = _currentTurn });
            PublishPhaseChanged(GamePhase.Prepare);

            var fieldCards = BoardManager.Instance.GetAllFieldCards();
            foreach (var card in fieldCards)
            {
                if (card == null || card.IsDestroyed)
                {
                    continue;
                }

                card.PreEffect?.Execute(card, GameManager.Instance.GameContext);
            }

            var config = GameManager.Instance.gameConfig;
            int drawCount = _currentTurn == 1 ? config.firstTurnDrawCount : config.normalTurnDrawCount;
            Deck.DeckManager.Instance.Draw(drawCount);

            BoardManager.Instance.CleanupDestroyedCards();
        }

        public void StartActionPhase()
        {
            if (_isGameOver)
            {
                return;
            }

            ActionPhaseEnded = false;
            PublishPhaseChanged(GamePhase.Action);
        }

        public CardPlayTargetKind GetRequiredTargetKind(CardInstance card)
        {
            return CardTargeting.GetRequiredTargetKind(card != null ? card.Data : null);
        }

        public CardPlayValidationResult ValidatePlay(CardInstance card, RoomSlot targetRoom = null)
        {
            CardPlayTargetKind requiredTargetKind = GetRequiredTargetKind(card);

            if (_isGameOver)
            {
                return CardPlayValidationResult.Failure(CardPlayBlockReason.GameOver, requiredTargetKind, targetRoom);
            }

            if (card == null || card.Data == null || card.IsDestroyed)
            {
                return CardPlayValidationResult.Failure(CardPlayBlockReason.InvalidTarget, requiredTargetKind, targetRoom);
            }

            if (CurrentPhase != GamePhase.Action || ActionPhaseEnded)
            {
                return CardPlayValidationResult.Failure(CardPlayBlockReason.NotActionPhase, requiredTargetKind, targetRoom);
            }

            if (!MoneyManager.Instance.CanAfford(card.Data.cost))
            {
                return CardPlayValidationResult.Failure(CardPlayBlockReason.InsufficientMoney, requiredTargetKind, targetRoom);
            }

            if (requiredTargetKind == CardPlayTargetKind.Room)
            {
                if (targetRoom == null)
                {
                    return CardPlayValidationResult.Failure(CardPlayBlockReason.MissingTarget, requiredTargetKind);
                }

                if (card.Data.cardType == CardType.Tenant)
                {
                    if (!targetRoom.CanPlaceTenant)
                    {
                        return CardPlayValidationResult.Failure(CardPlayBlockReason.TargetFull, requiredTargetKind, targetRoom);
                    }
                }
                else if (card.Data.cardType == CardType.Equipment)
                {
                    if (!targetRoom.CanPlaceEquipment)
                    {
                        return CardPlayValidationResult.Failure(CardPlayBlockReason.TargetFull, requiredTargetKind, targetRoom);
                    }
                }
            }

            return CardPlayValidationResult.Success(requiredTargetKind, targetRoom);
        }

        public bool PlayCard(CardInstance card, RoomSlot targetRoom = null)
        {
            var validation = ValidatePlay(card, targetRoom);
            if (!validation.IsValid)
            {
                return false;
            }

            targetRoom = validation.TargetRoom;
            var context = GameManager.Instance.GameContext;
            context.EffectContext.SelectedRoom = targetRoom;

            if (card.Data.cardType == CardType.Event)
            {
                return ResolveCardAfterPlay(card, context, null, null);
            }

            if (card.Data.cardType == CardType.Contract)
            {
                return ResolveCardAfterPlay(card, context, c => BoardManager.Instance.AddContract(c), null);
            }

            if (targetRoom == null || !targetRoom.PlaceCard(card))
            {
                Debug.LogWarning($"[TurnManager] Failed to place {card}");
                return false;
            }

            return ResolveCardAfterPlay(card, context, null, targetRoom);
        }

        public bool CardNeedsRoomTarget(CardInstance card)
        {
            return GetRequiredTargetKind(card) == CardPlayTargetKind.Room;
        }

        public void EndActionPhase()
        {
            if (_isGameOver)
            {
                return;
            }

            ActionPhaseEnded = true;
        }

        public void ExecuteSettlePhase()
        {
            if (_isGameOver)
            {
                return;
            }

            PublishPhaseChanged(GamePhase.Settle);

            var context = GameManager.Instance.GameContext;
            var toRemove = new List<CardInstance>();
            var toDestroy = new List<CardInstance>();

            var rooms = BoardManager.Instance.GetAllRooms();
            foreach (var room in rooms)
            {
                if (room.TenantCount <= 0)
                {
                    continue;
                }

                var roomCards = room.GetAllCards();
                var tenant = room.GetTenantAt(0);

                int baseRent = tenant != null ? Mathf.Max(0, tenant.Data.baseRent) : 0;
                if (baseRent > 0)
                {
                    MoneyManager.Instance.AddMoney(baseRent);
                }

                int moneyBeforeEffects = MoneyManager.Instance.CurrentMoney;
                foreach (var card in roomCards)
                {
                    if (card == null || card.IsDestroyed)
                    {
                        continue;
                    }

                    card.SettleEffect?.Execute(card, context);
                }

                int additiveAmount = MoneyManager.Instance.CurrentMoney - moneyBeforeEffects;
                PublishSettlementSequence(GameEvents.SettlementSourceKind.Room, room, tenant, baseRent, additiveAmount, DefaultSettlementMultiplier);

                foreach (var card in roomCards)
                {
                    if (card == null || card.IsDestroyed || card.Data.durability <= 0)
                    {
                        continue;
                    }

                    card.CurrentDurability--;
                    if (card.CurrentDurability <= 0)
                    {
                        toDestroy.Add(card);
                    }
                }
            }

            var contracts = BoardManager.Instance.GetAllContracts();
            foreach (var contract in contracts)
            {
                if (contract == null || contract.IsDestroyed)
                {
                    continue;
                }

                int moneyBeforeContract = MoneyManager.Instance.CurrentMoney;
                contract.SettleEffect?.Execute(contract, context);
                int contractDelta = MoneyManager.Instance.CurrentMoney - moneyBeforeContract;
                PublishSettlementSequence(GameEvents.SettlementSourceKind.Contract, null, contract, 0, contractDelta, DefaultSettlementMultiplier);

                if (contract.Data.durability <= 0)
                {
                    continue;
                }

                contract.CurrentDurability--;
                if (contract.CurrentDurability <= 0)
                {
                    toDestroy.Add(contract);
                }
            }

            var fieldCards = BoardManager.Instance.GetAllFieldCards();
            foreach (var card in fieldCards)
            {
                if (card == null || card.IsDestroyed || card.Data.waitTurns <= 0)
                {
                    continue;
                }

                card.CurrentWait--;
                if (card.CurrentWait <= 0)
                {
                    toRemove.Add(card);
                }
            }

            foreach (var card in toDestroy)
            {
                if (card == null || card.IsDestroyed)
                {
                    continue;
                }

                card.DestroyEffect?.Execute(card, context);
                card.MarkDestroyed();
                EventBus.Publish(new GameEvents.CardDestroyed
                {
                    Card = card,
                    TriggeredByDurability = true
                });
            }

            foreach (var card in toRemove)
            {
                if (card == null || card.IsDestroyed)
                {
                    continue;
                }

                card.MarkDestroyed();
                EventBus.Publish(new GameEvents.CardDestroyed
                {
                    Card = card,
                    TriggeredByDurability = false
                });
            }

            BoardManager.Instance.CleanupDestroyedCards();
            Deck.DeckManager.Instance.ResolveHandWaitAndDiscardExpired();

            var config = GameManager.Instance.gameConfig;
            if (config.loanInterval > 0 && _currentTurn % config.loanInterval == 0)
            {
                int requiredPayment = CalculateCurrentLoanPayment(config.loanAmount, config.loanGrowthFactor);
                bool paid = MoneyManager.Instance.ReduceMoney(requiredPayment);
                EventBus.Publish(new GameEvents.LoanPayment
                {
                    Amount = requiredPayment,
                    RemainingMoney = MoneyManager.Instance.CurrentMoney
                });

                if (!paid)
                {
                    _isGameOver = true;
                    EventBus.Publish(new GameEvents.GameOver
                    {
                        FinalMoney = MoneyManager.Instance.CurrentMoney,
                        TotalTurns = _currentTurn
                    });
                }
                else
                {
                    _loanPaymentCount++;
                    BaoZuPoFeedbackAdapter.PublishLoanPayment(requiredPayment);
                }
            }

            if (!_isGameOver)
            {
                bool boosted = config.loanInterval > 0 && _currentTurn % config.loanInterval == 0;
                AwardOneCardFromThreeOptions(boosted);
            }

            EventBus.Publish(new GameEvents.TurnEnded { TurnNumber = _currentTurn });
        }

        private bool ResolveCardAfterPlay(
            CardInstance card,
            GameContext context,
            System.Action<CardInstance> afterInstant,
            RoomSlot targetRoom)
        {
            if (!MoneyManager.Instance.ReduceMoney(card.Data.cost))
            {
                if (targetRoom != null && card != null && card.Data != null && card.Data.cardType != CardType.Event && card.Data.cardType != CardType.Contract)
                {
                    targetRoom.RemoveCard(card);
                }

                return false;
            }

            int moneyBeforeInstant = MoneyManager.Instance.CurrentMoney;
            card.InstantEffect?.Execute(card, context);
            int instantMoneyDelta = MoneyManager.Instance.CurrentMoney - moneyBeforeInstant;

            afterInstant?.Invoke(card);
            Deck.DeckManager.Instance.RemoveFromHand(card);

            EventBus.Publish(new GameEvents.CardPlayed { Card = card });
            BaoZuPoFeedbackAdapter.PublishPlayCost(card, targetRoom ?? card.PlacedRoom, card.Data.cost);
            PublishPlaySequence(card, targetRoom ?? card.PlacedRoom, instantMoneyDelta);
            return true;
        }

        private void PublishPlaySequence(CardInstance card, RoomSlot targetRoom, int moneyDelta)
        {
            if (card == null || moneyDelta == 0)
            {
                return;
            }

            BaoZuPoFeedbackAdapter.PublishInstantMoneyDelta(card, targetRoom, moneyDelta);
        }

        private void PublishPhaseChanged(GamePhase phase)
        {
            CurrentPhase = phase;
            EventBus.Publish(new GameEvents.PhaseChanged
            {
                Phase = phase,
                PhaseName = phase.ToString()
            });
        }

        private void PublishSettlementSequence(
            GameEvents.SettlementSourceKind sourceKind,
            RoomSlot room,
            CardInstance card,
            int baseAmount,
            int additiveAmount,
            float multiplier)
        {
            int preMultiplier = baseAmount + additiveAmount;
            int finalAmount = Mathf.RoundToInt(preMultiplier * multiplier);
            bool hasMultiplier = !Mathf.Approximately(multiplier, 1f);

            if (baseAmount == 0 && additiveAmount == 0 && !hasMultiplier && finalAmount == 0)
            {
                return;
            }

            var steps = new List<GameEvents.SettlementStep>(4);
            if (baseAmount != 0)
            {
                steps.Add(new GameEvents.SettlementStep
                {
                    Label = UIStrings.SettlementBase,
                    Amount = baseAmount,
                    IsMultiplier = false
                });
            }

            if (additiveAmount != 0)
            {
                steps.Add(new GameEvents.SettlementStep
                {
                    Label = UIStrings.SettlementBonus,
                    Amount = additiveAmount,
                    IsMultiplier = false
                });
            }

            if (hasMultiplier)
            {
                steps.Add(new GameEvents.SettlementStep
                {
                    Label = UIStrings.SettlementMultiplier,
                    Amount = Mathf.RoundToInt(multiplier * 100f),
                    IsMultiplier = true
                });
            }

            steps.Add(new GameEvents.SettlementStep
            {
                Label = UIStrings.SettlementFinal,
                Amount = finalAmount,
                IsMultiplier = false
            });

            EventBus.Publish(new GameEvents.SettlementSequenceQueued
            {
                SourceKind = sourceKind,
                Room = room,
                Card = card,
                Title = ResolveSettlementTitle(sourceKind, room, card),
                Steps = steps.ToArray(),
                FinalAmount = finalAmount
            });
        }

        private static string ResolveSettlementTitle(GameEvents.SettlementSourceKind sourceKind, RoomSlot room, CardInstance card)
        {
            return sourceKind switch
            {
                GameEvents.SettlementSourceKind.Room when room != null => UIStrings.SettlementRoomTitle(room.RoomIndex + 1),
                GameEvents.SettlementSourceKind.Contract when card != null => card.Data.cardName,
                GameEvents.SettlementSourceKind.Event when card != null => card.Data.cardName,
                _ => UIStrings.SettlementFallbackTitle
            };
        }

        private int CalculateCurrentLoanPayment(int baseAmount, float growthFactor)
        {
            int safeBase = Mathf.Max(0, baseAmount);
            float safeFactor = Mathf.Max(1f, growthFactor);
            float raw = safeBase * Mathf.Pow(safeFactor, _loanPaymentCount);
            return Mathf.RoundToInt(raw);
        }

        private void AwardOneCardFromThreeOptions(bool boosted)
        {
            var allCards = CardDatabase.GetAll().Values;
            var source = boosted
                ? allCards.Where(c => c.rarity >= CardRarity.Rare).ToList()
                : allCards.ToList();

            if (source.Count == 0)
            {
                Debug.LogWarning("[TurnManager] No reward cards available.");
                return;
            }

            var options = new List<CardData>();
            for (int i = 0; i < 3; i++)
            {
                options.Add(source[Random.Range(0, source.Count)]);
            }

            var chosen = options[Random.Range(0, options.Count)];
            Deck.DeckManager.Instance.AddCardToHand(chosen);
        }
    }
}

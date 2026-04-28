using System;
using System.Collections.Generic;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Deck;
using BaoZuPo.Economy;
using BaoZuPo.Integration.Martian.Feedback;
using Cysharp.Threading.Tasks;
using Martian.EventBus;
using Martian.Localization;
using UnityEngine;

namespace BaoZuPo.GameFlow
{
    public sealed class SettlementService : ISettlementService
    {
        public UniTask<SettlementResult> ResolveAsync(
            SettlementRequest request,
            System.Threading.CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(Resolve(request));
        }

        private SettlementResult Resolve(SettlementRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            EnsureGameManagerInitialized();

            var config = GameManager.Instance.gameConfig;
            var sharedContext = GameManager.Instance.GameContext;
            int sourceIndex = 0;
            var result = new SettlementResult
            {
                SettlementId = Guid.NewGuid().ToString("N"),
                TurnNumber = request.CurrentTurn,
                MoneyBefore = MoneyManager.Instance.CurrentMoney,
                NewLoanPaymentCount = request.LoanPaymentCount
            };

            var toDestroy = new List<CardInstance>();
            ProcessRoomSettlements(result, sharedContext, ref sourceIndex, toDestroy);
            ProcessContractSettlements(result, sharedContext, ref sourceIndex, toDestroy);
            DestroyAndCleanupCards(result, toDestroy, sharedContext, request.BeforeHandWaitCardsRemoved);
            ProcessLoanPayment(result, request.CurrentTurn, request.LoanPaymentCount);

            result.RewardBoosted = config.loanInterval > 0 && request.CurrentTurn % config.loanInterval == 0;
            result.MoneyAfter = MoneyManager.Instance.CurrentMoney;
            return result;
        }

        private static void ProcessRoomSettlements(
            SettlementResult result,
            GameContext sharedContext,
            ref int sourceIndex,
            List<CardInstance> toDestroy)
        {
            var rooms = BoardManager.Instance.GetAllRooms();
            foreach (var room in rooms)
            {
                if (room.TenantCount <= 0)
                {
                    continue;
                }

                AppendRoomSettlementStage(result, room, sharedContext, ref sourceIndex);

                foreach (var card in room.GetAllCards())
                {
                    if (card == null || card.IsDestroyed || card.Data.durability <= 0)
                    {
                        continue;
                    }

                    int before = card.CurrentDurability;
                    card.CurrentDurability--;
                    bool markedForDestroy = card.CurrentDurability <= 0;
                    result.DurabilityChanges.Add(new CardDurabilityChange
                    {
                        Card = card,
                        Room = room,
                        Before = before,
                        After = card.CurrentDurability,
                        MarkedForDestroy = markedForDestroy
                    });

                    if (markedForDestroy)
                    {
                        toDestroy.Add(card);
                    }
                }
            }
        }

        private static void ProcessContractSettlements(
            SettlementResult result,
            GameContext sharedContext,
            ref int sourceIndex,
            List<CardInstance> toDestroy)
        {
            var contracts = BoardManager.Instance.GetAllContracts();
            foreach (var contract in contracts)
            {
                if (contract == null || contract.IsDestroyed)
                {
                    continue;
                }

                int sourceStartMoney = MoneyManager.Instance.CurrentMoney;
                var contractContext = CreateSettlementExecutionContext(sharedContext, null);
                contractContext.SettlementCapture.Begin();
                contractContext.SettlementCapture.SourceCard = contract;
                contract.SettleEffect?.Execute(contract, contractContext);

                var source = CreateSettlementSource(
                    ref sourceIndex,
                    GameEvents.SettlementSourceKind.Contract,
                    null,
                    contract,
                    contractContext.SettlementCapture,
                    sourceStartMoney);

                if (source != null)
                {
                    var stage = new SettlementStageResult
                    {
                        DebugLabel = source.Title,
                        Kind = SettlementPlaybackStageKind.Serial
                    };
                    stage.Sources.Add(source);
                    result.Stages.Add(stage);
                }

                if (contract.Data.durability <= 0)
                {
                    continue;
                }

                int before = contract.CurrentDurability;
                contract.CurrentDurability--;
                bool markedForDestroy = contract.CurrentDurability <= 0;
                result.DurabilityChanges.Add(new CardDurabilityChange
                {
                    Card = contract,
                    Before = before,
                    After = contract.CurrentDurability,
                    MarkedForDestroy = markedForDestroy
                });

                if (markedForDestroy)
                {
                    toDestroy.Add(contract);
                }
            }
        }

        private static void AppendRoomSettlementStage(
            SettlementResult result,
            RoomSlot room,
            GameContext sharedContext,
            ref int sourceIndex)
        {
            if (room == null)
            {
                return;
            }

            var stage = new SettlementStageResult
            {
                DebugLabel = ResolveSettlementTitle(GameEvents.SettlementSourceKind.Room, room, null),
                Kind = SettlementPlaybackStageKind.Serial
            };

            var tenants = room.GetTenants();
            for (int i = 0; i < tenants.Count; i++)
            {
                var tenant = tenants[i];
                if (tenant == null || tenant.IsDestroyed)
                {
                    continue;
                }

                int sourceStartMoney = MoneyManager.Instance.CurrentMoney;
                var tenantContext = CreateSettlementExecutionContext(sharedContext, room);
                tenantContext.SettlementCapture.Begin();
                tenantContext.SettlementCapture.SourceCard = tenant;

                int baseRent = Mathf.Max(0, tenant.Data != null ? tenant.Data.baseRent : 0);
                if (baseRent > 0)
                {
                    MoneyManager.Instance.AddMoney(baseRent);
                    tenantContext.SettlementCapture.RecordBase(baseRent, ResolveGameText("GameText.SettlementBase", "Base"));
                }

                tenant.SettleEffect?.Execute(tenant, tenantContext);

                var source = CreateSettlementSource(
                    ref sourceIndex,
                    GameEvents.SettlementSourceKind.Room,
                    room,
                    tenant,
                    tenantContext.SettlementCapture,
                    sourceStartMoney);

                if (source != null)
                {
                    stage.Sources.Add(source);
                }
            }

            bool roomHasTidy = TagQuery.RoomHasTag(room, TagType.Tidy);
            var equipments = room.GetEquipments();
            for (int i = 0; i < equipments.Count; i++)
            {
                var equipment = equipments[i];
                if (equipment == null || equipment.IsDestroyed)
                {
                    continue;
                }

                int sourceStartMoney = MoneyManager.Instance.CurrentMoney;
                var equipmentContext = CreateSettlementExecutionContext(sharedContext, room);
                equipmentContext.SettlementCapture.Begin();
                equipmentContext.SettlementCapture.SourceCard = equipment;

                int moneyBeforeEquipment = MoneyManager.Instance.CurrentMoney;
                equipment.SettleEffect?.Execute(equipment, equipmentContext);

                if (roomHasTidy)
                {
                    int equipmentDelta = MoneyManager.Instance.CurrentMoney - moneyBeforeEquipment;
                    if (equipmentDelta > 0)
                    {
                        MoneyManager.Instance.AddMoney(equipmentDelta);
                        if (equipmentContext.SettlementCapture.IsCapturing)
                        {
                            equipmentContext.SettlementCapture.RecordDelta(equipmentDelta, ResolveGameText("GameText.TagTidy", "\u6574\u6d01\u52a0\u6210"));
                        }
                    }
                }

                var source = CreateSettlementSource(
                    ref sourceIndex,
                    GameEvents.SettlementSourceKind.Room,
                    room,
                    equipment,
                    equipmentContext.SettlementCapture,
                    sourceStartMoney);

                if (source != null)
                {
                    stage.Sources.Add(source);
                }
            }

            if (stage.Sources.Count > 0)
            {
                result.Stages.Add(stage);
            }
        }

        private static SettlementSourceResult CreateSettlementSource(
            ref int sourceIndex,
            GameEvents.SettlementSourceKind sourceKind,
            RoomSlot room,
            CardInstance card,
            SettlementCaptureContext capture,
            int sourceStartMoney)
        {
            if (capture == null)
            {
                return null;
            }

            int finalAmount = MoneyManager.Instance.CurrentMoney - sourceStartMoney;
            int capturedStepCount = capture.Steps.Count;
            var steps = capture.Complete(finalAmount, includeFinalStep: false);

            if (capturedStepCount == 0 && finalAmount == 0)
            {
                return null;
            }

            return new SettlementSourceResult
            {
                SourceIndex = sourceIndex++,
                SourceKind = sourceKind,
                Room = room,
                Card = card,
                Title = ResolveSettlementTitle(sourceKind, room, card),
                Steps = steps,
                FinalAmount = finalAmount
            };
        }

        private static void DestroyAndCleanupCards(
            SettlementResult result,
            List<CardInstance> toDestroy,
            GameContext sharedContext,
            Action<IReadOnlyList<CardInstance>> beforeHandWaitCardsRemoved)
        {
            foreach (var card in toDestroy)
            {
                if (card == null || card.IsDestroyed)
                {
                    continue;
                }

                int moneyBefore = MoneyManager.Instance.CurrentMoney;
                card.DestroyEffect?.Execute(card, sharedContext);
                int moneyAfter = MoneyManager.Instance.CurrentMoney;
                card.MarkDestroyed();
                result.DestroyedCards.Add(new CardDestroyedResult
                {
                    Card = card,
                    TriggeredByDurability = true,
                    MoneyBeforeDestroyEffect = moneyBefore,
                    MoneyAfterDestroyEffect = moneyAfter
                });

                EventBus.Publish(new GameEvents.CardDestroyed { Card = card, TriggeredByDurability = true });
            }

            BoardManager.Instance.CleanupDestroyedCards();

            var expiringHandCards = CollectExpiringHandCards();
            if (expiringHandCards.Count > 0)
            {
                result.HandCleanup.CardsDiscarded.AddRange(expiringHandCards);
                beforeHandWaitCardsRemoved?.Invoke(expiringHandCards);
            }

            DeckManager.Instance.ResolveHandWaitAndDiscardExpired();
        }

        private static List<CardInstance> CollectExpiringHandCards()
        {
            var expiringCards = new List<CardInstance>();
            if (DeckManager.Instance == null)
            {
                return expiringCards;
            }

            foreach (var card in DeckManager.Instance.Hand)
            {
                if (card == null || card.Data.waitTurns <= 0)
                {
                    continue;
                }

                if (card.CurrentWait == 1)
                {
                    expiringCards.Add(card);
                }
            }

            return expiringCards;
        }

        private static void ProcessLoanPayment(SettlementResult result, int currentTurn, int loanPaymentCount)
        {
            var config = GameManager.Instance.gameConfig;
            result.NewLoanPaymentCount = loanPaymentCount;
            result.Loan.PaymentIndexBefore = loanPaymentCount;
            result.Loan.PaymentIndexAfter = loanPaymentCount;

            if (config.loanInterval <= 0 || currentTurn % config.loanInterval != 0)
            {
                return;
            }

            int requiredPayment = CalculateCurrentLoanPayment(config.loanAmount, config.loanGrowthFactor, loanPaymentCount);
            bool paid = MoneyManager.Instance.ReduceMoney(requiredPayment);
            result.Loan.IsDue = true;
            result.Loan.Amount = requiredPayment;
            result.Loan.Paid = paid;
            result.Loan.RemainingMoney = MoneyManager.Instance.CurrentMoney;

            EventBus.Publish(new GameEvents.LoanPayment
            {
                Amount = requiredPayment,
                RemainingMoney = MoneyManager.Instance.CurrentMoney
            });

            if (!paid)
            {
                result.IsGameOver = true;
                result.GameOver = new GameOverResult
                {
                    IsGameOver = true,
                    FinalMoney = MoneyManager.Instance.CurrentMoney,
                    TotalTurns = currentTurn,
                    Reason = "LoanPaymentFailed"
                };

                EventBus.Publish(new GameEvents.GameOver
                {
                    FinalMoney = MoneyManager.Instance.CurrentMoney,
                    TotalTurns = currentTurn
                });
                return;
            }

            result.NewLoanPaymentCount = loanPaymentCount + 1;
            result.Loan.PaymentIndexAfter = result.NewLoanPaymentCount;
            BaoZuPoFeedbackAdapter.PublishLoanPayment(requiredPayment);
        }

        private static int CalculateCurrentLoanPayment(int baseAmount, float growthFactor, int loanPaymentCount)
        {
            int safeBase = Mathf.Max(0, baseAmount);
            float safeFactor = Mathf.Max(1f, growthFactor);
            float raw = safeBase * Mathf.Pow(safeFactor, loanPaymentCount);
            return Mathf.RoundToInt(raw);
        }

        private static string ResolveSettlementTitle(GameEvents.SettlementSourceKind sourceKind, RoomSlot room, CardInstance card)
        {
            return sourceKind switch
            {
                GameEvents.SettlementSourceKind.Room when card != null => ResolveCardName(card),
                GameEvents.SettlementSourceKind.Room when room != null => ResolveGameText("GameText.SettlementRoomTitle", $"Room {room.RoomIndex + 1}", room.RoomIndex + 1),
                GameEvents.SettlementSourceKind.Contract when card != null => ResolveCardName(card),
                GameEvents.SettlementSourceKind.Event when card != null => ResolveCardName(card),
                _ => ResolveGameText("GameText.SettlementFallbackTitle", "Settle")
            };
        }

        private static string ResolveCardName(CardInstance card)
        {
            var data = card != null ? card.Data : null;
            if (data == null)
            {
                return string.Empty;
            }

            return LocalizationServices.Resolve(
                new LocalizationTextRef("Card", $"Card.{data.cardId}.Name", data.cardName ?? string.Empty));
        }

        private static string ResolveGameText(string entry, string fallback, params object[] arguments)
        {
            return LocalizationServices.Resolve(new LocalizationTextRef("GameText", entry, fallback), arguments);
        }

        private static GameContext CreateSettlementExecutionContext(GameContext sharedContext, RoomSlot selectedRoom)
        {
            var context = new GameContext
            {
                MoneyManager = sharedContext != null ? sharedContext.MoneyManager : null,
                BoardManager = sharedContext != null ? sharedContext.BoardManager : null
            };

            context.EffectContext.SelectedRoom = selectedRoom;
            return context;
        }

        private static void EnsureGameManagerInitialized()
        {
            GameManager.Instance?.EnsureInitialized();
        }
    }
}

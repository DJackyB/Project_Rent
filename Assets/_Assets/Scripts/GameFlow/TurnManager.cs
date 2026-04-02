using System;
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
using BaoZuPo.UI.Settlement;

namespace BaoZuPo.GameFlow
{
    public class TurnManager : Singleton<TurnManager>
    {
        private const string DefaultSettlementLaneKey = "settlement-global";

        [Header("Debug")]
        [SerializeField] private int _currentTurn;
        [SerializeField] private bool _isGameOver;
        [SerializeField] private int _loanPaymentCount;

        private string _activeSettlementBatchId;
        private int _pendingSettlementPlaybackCount;
        private bool _settlementTurnEndedPublished;
        private bool _isRewardSelectionPending;
        private bool _eventsSubscribed;

        // 缓存当前回合的 boosted 状态，供结算动画完成后使用
        private bool _pendingRewardBoosted;

        public int CurrentTurn => _currentTurn;
        public bool IsGameOver => _isGameOver;
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Prepare;
        public bool ActionPhaseEnded { get; set; }
        public bool IsSettlementPlaybackPending => _pendingSettlementPlaybackCount > 0;
        public bool IsRewardSelectionPending => _isRewardSelectionPending;
        public string ActiveSettlementBatchId => _activeSettlementBatchId;

        private void OnEnable()
        {
            EnsureEventSubscriptions();
        }

        private void OnDisable()
        {
            if (!_eventsSubscribed)
            {
                return;
            }

            EventBus.Unsubscribe<GameEvents.SettlementPlaybackCompleted>(OnSettlementPlaybackCompleted);
            EventBus.Unsubscribe<GameEvents.CardRewardSelected>(OnCardRewardSelected);
            _eventsSubscribed = false;
        }

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
            var drawLibrary = _currentTurn == 1 ? config.firstTurnDrawLibrary : config.normalTurnDrawLibrary;
            Deck.DeckManager.Instance.DrawFromLibrary(drawLibrary, drawCount);

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

            if (CardTargeting.PersistsAsContract(card.Data))
            {
                return ResolveCardAfterPlay(card, context, c => BoardManager.Instance.AddContract(c), targetRoom);
            }

            if (CardTargeting.PersistsInRoom(card.Data))
            {
                if (targetRoom == null || !targetRoom.PlaceCard(card))
                {
                    Debug.LogWarning($"[TurnManager] Failed to place {card}");
                    return false;
                }
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

            var sharedContext = GameManager.Instance.GameContext;
            int phaseStartMoney = MoneyManager.Instance.CurrentMoney;
            string batchId = Guid.NewGuid().ToString("N");
            int sourceIndex = 0;
            var settlementBatch = new UISettlementPlaybackBatch
            {
                CompletionBatchId = batchId,
                DeferredMoneyStartValue = phaseStartMoney
            };

            var toRemove = new List<CardInstance>();
            var toDestroy = new List<CardInstance>();

            ProcessRoomSettlements(settlementBatch, batchId, sharedContext, ref sourceIndex, toDestroy);
            ProcessContractSettlements(settlementBatch, batchId, sharedContext, ref sourceIndex, toDestroy);
            ProcessWaitExpiry(toRemove);
            DestroyAndCleanupCards(toDestroy, toRemove, sharedContext);

            ProcessLoanPayment();

            // 缓存 boosted 状态，奖励在结算动画播完后再展示
            var config = GameManager.Instance.gameConfig;
            _pendingRewardBoosted = config.loanInterval > 0 && _currentTurn % config.loanInterval == 0;

            FinalizeBatch(settlementBatch, batchId);
        }

        private void ProcessRoomSettlements(
            UISettlementPlaybackBatch batch,
            string batchId,
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

                AppendRoomSettlementStage(batch, batchId, room, sharedContext, ref sourceIndex);

                foreach (var card in room.GetAllCards())
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
        }

        private void ProcessContractSettlements(
            UISettlementPlaybackBatch batch,
            string batchId,
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
                contract.SettleEffect?.Execute(contract, contractContext);

                var payload = CreateSettlementPayload(
                    batchId,
                    ref sourceIndex,
                    GameEvents.SettlementSourceKind.Contract,
                    null,
                    contract,
                    contractContext.SettlementCapture,
                    sourceStartMoney);

                if (payload != null)
                {
                    payload.TrackIndex = 0;
                    payload.TrackCount = 1;
                    payload.LaneKey = BuildLaneKey(batchId, payload.SourceIndex);
                    batch.Stages.Add(UISettlementPlaybackStage.CreateSerial(
                        payload.Title,
                        UISettlementPlaybackEntry.Create(payload, payload.LaneKey)));
                }

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
        }

        private static void ProcessWaitExpiry(List<CardInstance> toRemove)
        {
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
        }

        private static void DestroyAndCleanupCards(
            List<CardInstance> toDestroy,
            List<CardInstance> toRemove,
            GameContext sharedContext)
        {
            foreach (var card in toDestroy)
            {
                if (card == null || card.IsDestroyed)
                {
                    continue;
                }

                card.DestroyEffect?.Execute(card, sharedContext);
                card.MarkDestroyed();
                EventBus.Publish(new GameEvents.CardDestroyed { Card = card, TriggeredByDurability = true });
            }

            foreach (var card in toRemove)
            {
                if (card == null || card.IsDestroyed)
                {
                    continue;
                }

                card.MarkDestroyed();
                EventBus.Publish(new GameEvents.CardDestroyed { Card = card, TriggeredByDurability = false });
            }

            BoardManager.Instance.CleanupDestroyedCards();
            Deck.DeckManager.Instance.ResolveHandWaitAndDiscardExpired();
        }

        private void ProcessLoanPayment()
        {
            var config = GameManager.Instance.gameConfig;
            if (config.loanInterval <= 0 || _currentTurn % config.loanInterval != 0)
            {
                return;
            }

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

        private void FinalizeBatch(UISettlementPlaybackBatch settlementBatch, string batchId)
        {
            settlementBatch.DeferredMoneyEndValue = MoneyManager.Instance.CurrentMoney;
            FinalizeSourceCounts(settlementBatch);

            if (settlementBatch.IsEmpty && settlementBatch.TotalDelta == 0)
            {
                // 无结算动画，直接进入奖励或完成
                TryStartRewardOrComplete();
                return;
            }

            if (UIManager.Instance == null)
            {
                TryStartRewardOrComplete();
                return;
            }

            BeginSettlementPlayback(batchId, 1);
            UIManager.Instance.SubmitSettlementBatch(settlementBatch);
        }

        public void NotifySettlementPlaybackCompleted(string batchId)
        {
            OnSettlementPlaybackCompleted(new GameEvents.SettlementPlaybackCompleted
            {
                BatchId = batchId
            });
        }

        private bool ResolveCardAfterPlay(
            CardInstance card,
            GameContext context,
            Action<CardInstance> afterInstant,
            RoomSlot targetRoom)
        {
            if (!MoneyManager.Instance.ReduceMoney(card.Data.cost))
            {
                if (targetRoom != null && card != null && card.PlacedRoom == targetRoom)
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

        private void AppendRoomSettlementStage(
            UISettlementPlaybackBatch batch,
            string batchId,
            RoomSlot room,
            GameContext sharedContext,
            ref int sourceIndex)
        {
            if (batch == null || room == null)
            {
                return;
            }

            var roomPayloads = new List<GameEvents.SettlementSequenceQueued>();

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

                int baseRent = Mathf.Max(0, tenant.Data != null ? tenant.Data.baseRent : 0);
                if (baseRent > 0)
                {
                    MoneyManager.Instance.AddMoney(baseRent);
                    tenantContext.SettlementCapture.RecordBase(baseRent, GameText.SettlementBase);
                }

                tenant.SettleEffect?.Execute(tenant, tenantContext);

                var payload = CreateSettlementPayload(
                    batchId,
                    ref sourceIndex,
                    GameEvents.SettlementSourceKind.Room,
                    room,
                    tenant,
                    tenantContext.SettlementCapture,
                    sourceStartMoney);

                if (payload != null)
                {
                    roomPayloads.Add(payload);
                }
            }

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
                equipment.SettleEffect?.Execute(equipment, equipmentContext);

                var payload = CreateSettlementPayload(
                    batchId,
                    ref sourceIndex,
                    GameEvents.SettlementSourceKind.Room,
                    room,
                    equipment,
                    equipmentContext.SettlementCapture,
                    sourceStartMoney);

                if (payload != null)
                {
                    roomPayloads.Add(payload);
                }
            }

            if (roomPayloads.Count == 0)
            {
                return;
            }

            var entries = new UISettlementPlaybackEntry[roomPayloads.Count];
            for (int i = 0; i < roomPayloads.Count; i++)
            {
                var payload = roomPayloads[i];
                payload.TrackIndex = i;
                payload.TrackCount = roomPayloads.Count;
                payload.LaneKey = BuildLaneKey(batchId, payload.SourceIndex);
                entries[i] = UISettlementPlaybackEntry.Create(payload, payload.LaneKey);
            }

            batch.Stages.Add(UISettlementPlaybackStage.CreateParallel(
                ResolveSettlementTitle(GameEvents.SettlementSourceKind.Room, room, null),
                entries));
        }

        private GameEvents.SettlementSequenceQueued CreateSettlementPayload(
            string batchId,
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

            return new GameEvents.SettlementSequenceQueued
            {
                BatchId = batchId,
                SourceIndex = sourceIndex++,
                SourceCount = 0,
                LaneKey = DefaultSettlementLaneKey,
                SourceKind = sourceKind,
                Room = room,
                Card = card,
                Title = ResolveSettlementTitle(sourceKind, room, card),
                Steps = steps,
                FinalAmount = finalAmount,
                TrackIndex = 0,
                TrackCount = 1
            };
        }

        private void BeginSettlementPlayback(string batchId, int pendingCount)
        {
            _activeSettlementBatchId = batchId;
            _pendingSettlementPlaybackCount = Mathf.Max(0, pendingCount);
            _settlementTurnEndedPublished = false;
        }

        private void OnSettlementPlaybackCompleted(GameEvents.SettlementPlaybackCompleted e)
        {
            if (string.IsNullOrWhiteSpace(e.BatchId) || !string.Equals(e.BatchId, _activeSettlementBatchId, StringComparison.Ordinal))
            {
                return;
            }

            if (_pendingSettlementPlaybackCount > 0)
            {
                _pendingSettlementPlaybackCount--;
            }

            if (_pendingSettlementPlaybackCount <= 0)
            {
                // 结算动画全部播完，进入奖励选择或直接完成
                TryStartRewardOrComplete();
            }
        }

        /// <summary>
        /// 结算动画播完后，决定是展示三选一奖励还是直接结束回合。
        /// </summary>
        private void TryStartRewardOrComplete()
        {
            if (!_isGameOver)
            {
                AwardOneCardFromThreeOptions(_pendingRewardBoosted);
                // 如果 AwardOneCardFromThreeOptions 设置了 _isRewardSelectionPending，
                // 则等待玩家选择；否则（无可用卡）直接完成
                if (_isRewardSelectionPending)
                {
                    return;
                }
            }

            CompleteSettlementPhase();
        }

        private void OnCardRewardSelected(GameEvents.CardRewardSelected e)
        {
            if (!_isRewardSelectionPending)
            {
                return;
            }

            if (e.ChosenCard != null)
            {
                Deck.DeckManager.Instance.AddCardToHand(e.ChosenCard);
            }

            _isRewardSelectionPending = false;
            CompleteSettlementPhase();
        }

        private void CompleteSettlementPhase()
        {
            if (_settlementTurnEndedPublished || _isRewardSelectionPending)
            {
                return;
            }

            _settlementTurnEndedPublished = true;
            _pendingSettlementPlaybackCount = 0;
            _activeSettlementBatchId = null;
            EventBus.Publish(new GameEvents.TurnEnded { TurnNumber = _currentTurn });
        }

        private static string ResolveSettlementTitle(GameEvents.SettlementSourceKind sourceKind, RoomSlot room, CardInstance card)
        {
            return sourceKind switch
            {
                GameEvents.SettlementSourceKind.Room when card != null => card.Data.cardName,
                GameEvents.SettlementSourceKind.Room when room != null => GameText.SettlementRoomTitle(room.RoomIndex + 1),
                GameEvents.SettlementSourceKind.Contract when card != null => card.Data.cardName,
                GameEvents.SettlementSourceKind.Event when card != null => card.Data.cardName,
                _ => GameText.SettlementFallbackTitle
            };
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

        private static string BuildLaneKey(string batchId, int sourceIndex)
        {
            return $"{DefaultSettlementLaneKey}:{batchId}:{sourceIndex}";
        }

        private static void FinalizeSourceCounts(UISettlementPlaybackBatch batch)
        {
            if (batch == null || batch.Stages == null)
            {
                return;
            }

            int totalSourceCount = 0;
            for (int i = 0; i < batch.Stages.Count; i++)
            {
                var stage = batch.Stages[i];
                if (stage == null || stage.Entries == null)
                {
                    continue;
                }

                for (int j = 0; j < stage.Entries.Count; j++)
                {
                    if (stage.Entries[j] != null && stage.Entries[j].Payload != null)
                    {
                        totalSourceCount++;
                    }
                }
            }

            for (int i = 0; i < batch.Stages.Count; i++)
            {
                var stage = batch.Stages[i];
                if (stage == null || stage.Entries == null)
                {
                    continue;
                }

                for (int j = 0; j < stage.Entries.Count; j++)
                {
                    if (stage.Entries[j] != null && stage.Entries[j].Payload != null)
                    {
                        stage.Entries[j].Payload.SourceCount = totalSourceCount;
                    }
                }
            }
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
            EnsureEventSubscriptions();
            var rewardLibrary = GameManager.Instance != null && GameManager.Instance.gameConfig != null
                ? GameManager.Instance.gameConfig.rewardLibrary
                : null;

            if (rewardLibrary == null)
            {
                Debug.LogWarning("[TurnManager] No reward library configured.");
                return;
            }

            var allCards = rewardLibrary.cards;
            List<CardData> source;
            if (boosted)
            {
                source = allCards.Where(c => c.rarity >= CardRarity.Rare).ToList();
                if (source.Count == 0)
                {
                    Debug.LogWarning("[TurnManager] Boosted reward requested but reward library has no Rare+ cards. Falling back to full reward library.");
                    source = allCards.ToList();
                }
            }
            else
            {
                source = allCards.ToList();
            }

            if (source.Count == 0)
            {
                Debug.LogWarning("[TurnManager] No reward cards available.");
                return;
            }

            if (UIManager.Instance == null)
            {
                throw new InvalidOperationException("[TurnManager] Reward selection requires UIManager in scene.");
            }

            var options = BuildUniqueRewardOptions(source, 3);
            if (options.Length == 0)
            {
                Debug.LogWarning("[TurnManager] No unique reward cards available.");
                return;
            }

            if (options.Length < 3)
            {
                Debug.LogWarning($"[TurnManager] Reward library only has {options.Length} unique reward card(s) available. Offering {options.Length} unique option(s).");
            }

            // 设置等待状态，由 UI 发布 CardRewardSelected 事件后恢复
            _isRewardSelectionPending = true;
            EventBus.Publish(new GameEvents.CardRewardOffered
            {
                Options = options,
                Boosted = boosted
            });
        }

        private void EnsureEventSubscriptions()
        {
            if (_eventsSubscribed)
            {
                return;
            }

            EventBus.Subscribe<GameEvents.SettlementPlaybackCompleted>(OnSettlementPlaybackCompleted);
            EventBus.Subscribe<GameEvents.CardRewardSelected>(OnCardRewardSelected);
            _eventsSubscribed = true;
        }

        private static CardData[] BuildUniqueRewardOptions(List<CardData> source, int desiredCount)
        {
            var remaining = source != null
                ? source.Where(card => card != null).ToList()
                : new List<CardData>();
            var options = new List<CardData>(Mathf.Max(0, desiredCount));

            while (remaining.Count > 0 && options.Count < desiredCount)
            {
                var chosen = remaining[UnityEngine.Random.Range(0, remaining.Count)];
                options.Add(chosen);
                remaining.RemoveAll(card => IsSameRewardCard(card, chosen));
            }

            return options.ToArray();
        }

        private static bool IsSameRewardCard(CardData left, CardData right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return ReferenceEquals(left, right) || left.cardId == right.cardId;
        }
    }
}

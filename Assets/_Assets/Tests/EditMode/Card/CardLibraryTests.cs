using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Deck;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using BaoZuPo.UI;
using Martian.EventBus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BaoZuPo.Tests.Card
{
    public class CardLibraryTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new();

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            CardDatabase.Clear();
            CardLibraryDatabase.Clear();
            CardEffectFactory.ClearAll();
            CardEffectRegistration.EnsureRegistered();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ClearAll();
            CardDatabase.Clear();
            CardLibraryDatabase.Clear();
            CardEffectFactory.ClearAll();

            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void LoadAll_ContainsDefaultAllCardsLibrary()
        {
            CardLibraryDatabase.LoadAll();

            var library = CardLibraryDatabase.GetById("AllCards");

            Assert.NotNull(library);
            Assert.AreEqual(50, library.cards.Count);
        }

        [Test]
        public void LoadAll_ContainsExpectedConfiguredLibraries()
        {
            CardLibraryDatabase.LoadAll();

            var libraries = CardLibraryDatabase.GetAll();

            CollectionAssert.IsSupersetOf(libraries.Keys, new[] { "AllCards", "0", "1", "2" });
            Assert.AreEqual(4, libraries.Count);
        }

        [Test]
        public void CardDatabase_LoadAll_HasExpectedCardCountAndTypeDistribution()
        {
            CardDatabase.LoadAll();

            var allCards = CardDatabase.GetAll().Values.ToArray();

            Assert.AreEqual(50, allCards.Length);
            Assert.AreEqual(20, allCards.Count(card => card.cardType == CardType.Tenant));
            Assert.AreEqual(13, allCards.Count(card => card.cardType == CardType.Equipment));
            Assert.AreEqual(13, allCards.Count(card => card.cardType == CardType.Event));
            Assert.AreEqual(4, allCards.Count(card => card.cardType == CardType.Contract));
        }

        [Test]
        public void Register_RejectsDuplicateLibraryIds()
        {
            var first = CreateLibrary("DuplicatePool", CreateCardData(1));
            var second = CreateLibrary("DuplicatePool", CreateCardData(2));

            CardLibraryDatabase.Register(first);

            var exception = Assert.Throws<InvalidOperationException>(() => CardLibraryDatabase.Register(second));
            StringAssert.Contains("Duplicate libraryId", exception.Message);
        }

        [Test]
        public void Register_RejectsNullCardEntries()
        {
            var library = CreateLibrary("NullEntryPool", new CardData[] { null });

            var exception = Assert.Throws<InvalidOperationException>(() => CardLibraryDatabase.Register(library));
            StringAssert.Contains("null card entry", exception.Message);
        }

        [Test]
        public void DrawCardValidation_SupportsOptionalLibraryArgument()
        {
            CardLibraryDatabase.Register(CreateLibrary("EventPool", CreateCardData(10)));

            Assert.IsTrue(CardEffectFactory.TryValidate("DrawCard;2", out var defaultError), defaultError);
            Assert.IsTrue(CardEffectFactory.TryValidate("DrawCard;2;EventPool", out var libraryError), libraryError);
        }

        [Test]
        public void DrawCardValidation_RejectsMissingLibraryWhenDatabaseIsLoaded()
        {
            CardLibraryDatabase.Register(CreateLibrary("KnownPool", CreateCardData(11)));

            Assert.IsFalse(CardEffectFactory.TryValidate("DrawCard;2;MissingPool", out var error));
            StringAssert.Contains("does not exist", error);
        }

        [Test]
        public void EffectValidation_SupportsNewCardExpansionEffects()
        {
            Assert.IsTrue(CardEffectFactory.TryValidate("AddRoom", out var addRoomError), addRoomError);
            Assert.IsTrue(
                CardEffectFactory.TryValidate("AddMoneyBySelectedRoomTenantCount;30", out var selectedRoomMoneyError),
                selectedRoomMoneyError);
        }

        [Test]
        public void CardInstance_ThrowsWhenEffectsAreNotRegistered()
        {
            CardEffectFactory.ClearAll();
            var card = CreateCardData(12, "Unregistered Effect Card");
            card.instantEffect = "AddMoney;10";

            var exception = Assert.Throws<InvalidOperationException>(() => new CardInstance(card));
            StringAssert.Contains("Card effects are not registered", exception.Message);
        }

        [Test]
        public void DrawFromLibrary_UsesSpecifiedLibrary_AndHonorsHandCap()
        {
            var deckManager = CreateComponent<DeckManager>("DeckManager");
            deckManager.Initialize(maxHandSize: 1);

            var libraryCard = CreateCardData(20);
            var library = CreateLibrary("CapPool", libraryCard);

            var drawn = deckManager.DrawFromLibrary(library, 3);

            Assert.AreEqual(1, drawn.Count);
            Assert.AreEqual(1, deckManager.HandCount);
            Assert.AreSame(libraryCard, drawn[0].Data);
            Assert.AreSame(libraryCard, deckManager.Hand[0].Data);
        }

        [Test]
        public void ExecutePreparePhase_UsesFirstTurnThenNormalTurnLibraries()
        {
            var firstCard = CreateCardData(30, "First Turn");
            var normalCard = CreateCardData(31, "Normal Turn");

            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", firstCard),
                CreateLibrary("NormalPool", normalCard),
                CreateLibrary("RewardPool", CreateCardData(32, "Reward")),
                firstTurnDrawCount: 1,
                normalTurnDrawCount: 1,
                maxHandSize: 5);

            context.TurnManager.ExecutePreparePhase();
            Assert.AreEqual(1, context.DeckManager.HandCount);
            Assert.AreSame(firstCard, context.DeckManager.Hand[0].Data);

            context.TurnManager.ExecutePreparePhase();
            Assert.AreEqual(2, context.DeckManager.HandCount);
            Assert.AreSame(normalCard, context.DeckManager.Hand[1].Data);
        }

        [Test]
        public void RewardGeneration_OffersThreeUniqueOptions_WhenRewardLibraryContainsDistinctCards()
        {
            var rewardA = CreateCardData(40, "Reward A");
            var rewardB = CreateCardData(41, "Reward B");
            var rewardC = CreateCardData(42, "Reward C");
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(43, "First")),
                CreateLibrary("NormalPool", CreateCardData(44, "Normal")),
                CreateLibrary("RewardPool", rewardA, rewardB, rewardC),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            GameEvents.CardRewardOffered? offered = null;
            void OnRewardOffered(GameEvents.CardRewardOffered e) => offered = e;
            EventBus.Subscribe<GameEvents.CardRewardOffered>(OnRewardOffered);

            try
            {
                InvokePrivateMethod(context.TurnManager, "AwardOneCardFromThreeOptions", false);
            }
            finally
            {
                EventBus.Unsubscribe<GameEvents.CardRewardOffered>(OnRewardOffered);
            }

            Assert.IsTrue(offered.HasValue);
            Assert.AreEqual(3, offered.Value.Options.Length);
            CollectionAssert.AreEquivalent(new[] { rewardA, rewardB, rewardC }, offered.Value.Options);
            Assert.AreEqual(0, context.DeckManager.HandCount);
            Assert.IsTrue(context.TurnManager.IsRewardSelectionPending);
        }

        [Test]
        public void RewardSelection_AddsChosenCardToHand_AndClearsPending()
        {
            var rewardA = CreateCardData(50, "Reward A");
            var rewardB = CreateCardData(51, "Reward B");
            var rewardC = CreateCardData(52, "Reward C");
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(53, "First")),
                CreateLibrary("NormalPool", CreateCardData(54, "Normal")),
                CreateLibrary("RewardPool", rewardA, rewardB, rewardC),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            InvokePrivateMethod(context.TurnManager, "AwardOneCardFromThreeOptions", false);
            EventBus.Publish(new GameEvents.CardRewardSelected { ChosenCard = rewardA });

            Assert.AreEqual(1, context.DeckManager.HandCount);
            Assert.AreSame(rewardA, context.DeckManager.Hand[0].Data);
            Assert.IsFalse(context.TurnManager.IsRewardSelectionPending);
        }

        [Test]
        public void RewardSelection_SkipClearsPending_WithoutAddingCard()
        {
            var rewardA = CreateCardData(60, "Reward A");
            var rewardB = CreateCardData(61, "Reward B");
            var rewardC = CreateCardData(62, "Reward C");
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(63, "First")),
                CreateLibrary("NormalPool", CreateCardData(64, "Normal")),
                CreateLibrary("RewardPool", rewardA, rewardB, rewardC),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            InvokePrivateMethod(context.TurnManager, "AwardOneCardFromThreeOptions", false);
            EventBus.Publish(new GameEvents.CardRewardSelected { ChosenCard = null });

            Assert.AreEqual(0, context.DeckManager.HandCount);
            Assert.IsFalse(context.TurnManager.IsRewardSelectionPending);
        }

        [Test]
        public void RewardGeneration_BoostedWithoutRareCards_LogsWarning_AndFallsBackToFullPool()
        {
            var commonA = CreateCardData(70, "Common Reward A");
            var commonB = CreateCardData(71, "Common Reward B");
            var commonC = CreateCardData(72, "Common Reward C");
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(73, "First")),
                CreateLibrary("NormalPool", CreateCardData(74, "Normal")),
                CreateLibrary("RewardPool", commonA, commonB, commonC),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            GameEvents.CardRewardOffered? offered = null;
            void OnRewardOffered(GameEvents.CardRewardOffered e) => offered = e;
            EventBus.Subscribe<GameEvents.CardRewardOffered>(OnRewardOffered);

            try
            {
                LogAssert.Expect(LogType.Warning, "[TurnManager] Boosted reward requested but reward library has no Rare+ cards. Falling back to full reward library.");
                InvokePrivateMethod(context.TurnManager, "AwardOneCardFromThreeOptions", true);
            }
            finally
            {
                EventBus.Unsubscribe<GameEvents.CardRewardOffered>(OnRewardOffered);
            }

            Assert.IsTrue(offered.HasValue);
            Assert.AreEqual(3, offered.Value.Options.Length);
            CollectionAssert.AreEquivalent(new[] { commonA, commonB, commonC }, offered.Value.Options);
            Assert.IsTrue(offered.Value.Boosted);
        }

        [Test]
        public void RewardGeneration_DeduplicatesWeightedRewardEntries()
        {
            var rewardA = CreateCardData(100, "Reward A");
            var rewardB = CreateCardData(101, "Reward B");
            var rewardC = CreateCardData(102, "Reward C");
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(103, "First")),
                CreateLibrary("NormalPool", CreateCardData(104, "Normal")),
                CreateLibrary("RewardPool", rewardA, rewardA, rewardB, rewardB, rewardC),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            GameEvents.CardRewardOffered? offered = null;
            void OnRewardOffered(GameEvents.CardRewardOffered e) => offered = e;
            EventBus.Subscribe<GameEvents.CardRewardOffered>(OnRewardOffered);

            try
            {
                InvokePrivateMethod(context.TurnManager, "AwardOneCardFromThreeOptions", false);
            }
            finally
            {
                EventBus.Unsubscribe<GameEvents.CardRewardOffered>(OnRewardOffered);
            }

            Assert.IsTrue(offered.HasValue);
            Assert.AreEqual(3, offered.Value.Options.Length);
            CollectionAssert.AreEquivalent(new[] { rewardA, rewardB, rewardC }, offered.Value.Options);
        }

        [Test]
        public void RewardGeneration_WithFewerThanThreeUniqueCards_LogsWarning_AndOffersAvailableUniqueOptions()
        {
            var rewardA = CreateCardData(110, "Reward A");
            var rewardB = CreateCardData(111, "Reward B");
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(112, "First")),
                CreateLibrary("NormalPool", CreateCardData(113, "Normal")),
                CreateLibrary("RewardPool", rewardA, rewardA, rewardB),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            GameEvents.CardRewardOffered? offered = null;
            void OnRewardOffered(GameEvents.CardRewardOffered e) => offered = e;
            EventBus.Subscribe<GameEvents.CardRewardOffered>(OnRewardOffered);

            try
            {
                LogAssert.Expect(LogType.Warning, "[TurnManager] Reward library only has 2 unique reward card(s) available. Offering 2 unique option(s).");
                InvokePrivateMethod(context.TurnManager, "AwardOneCardFromThreeOptions", false);
            }
            finally
            {
                EventBus.Unsubscribe<GameEvents.CardRewardOffered>(OnRewardOffered);
            }

            Assert.IsTrue(offered.HasValue);
            Assert.AreEqual(2, offered.Value.Options.Length);
            CollectionAssert.AreEquivalent(new[] { rewardA, rewardB }, offered.Value.Options);
        }

        [Test]
        public void TargetValidation_AllowsTenantCardsToUsePlayArea()
        {
            var card = CreateCardData(120, "Portable Tenant");
            card.cardType = CardType.Tenant;
            card.targetKind = CardPlayTargetKind.PlayArea;

            Assert.IsTrue(CardTargeting.TryValidateConfiguredTargetKind(card, out var warning), warning);
            Assert.AreEqual(CardPlayTargetKind.PlayArea, CardTargeting.GetRequiredTargetKind(card));
        }

        [Test]
        public void TargetValidation_RejectsSelectedRoomEffectsWithoutRoomTarget()
        {
            var card = CreateCardData(121, "Broken Target");
            card.cardType = CardType.Event;
            card.targetKind = CardPlayTargetKind.PlayArea;
            card.instantEffect = "AddTenantDurabilityInSelectedRoom;2";

            Assert.IsFalse(CardTargeting.TryValidateConfiguredTargetKind(card, out var warning));
            StringAssert.Contains("room-dependent", warning);
        }

        [Test]
        public void TargetValidation_RejectsExpandSlotWithoutRoomTarget()
        {
            var card = CreateCardData(1220, "Broken Expand");
            card.cardType = CardType.Event;
            card.targetKind = CardPlayTargetKind.PlayArea;
            card.instantEffect = "ExpandSlot;1";

            Assert.IsFalse(CardTargeting.TryValidateConfiguredTargetKind(card, out var warning));
            StringAssert.Contains("room-dependent", warning);
        }

        [Test]
        public void TargetValidation_RejectsMoveTenantWithoutRoomTarget()
        {
            var card = CreateCardData(1221, "Broken Move");
            card.cardType = CardType.Event;
            card.targetKind = CardPlayTargetKind.PlayArea;
            card.instantEffect = "MoveTenantToEmptyRoom";

            Assert.IsFalse(CardTargeting.TryValidateConfiguredTargetKind(card, out var warning));
            StringAssert.Contains("room-dependent", warning);
        }

        [Test]
        public void PlayCard_TenantWithPlayAreaTarget_ResolvesWithoutRoomPlacement()
        {
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(122, "First")),
                CreateLibrary("NormalPool", CreateCardData(123, "Normal")),
                CreateLibrary("RewardPool", CreateCardData(124, "Reward")),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            var portableTenant = CreateCardData(125, "Portable Tenant");
            portableTenant.cardType = CardType.Tenant;
            portableTenant.targetKind = CardPlayTargetKind.PlayArea;
            portableTenant.instantEffect = "AddMoney;25";

            context.DeckManager.AddCardToHand(portableTenant);
            var card = context.DeckManager.Hand[0];

            context.TurnManager.StartActionPhase();

            var validation = context.TurnManager.ValidatePlay(card);
            Assert.IsTrue(validation.IsValid);
            Assert.AreEqual(CardPlayTargetKind.PlayArea, validation.RequiredTargetKind);

            bool played = context.TurnManager.PlayCard(card);

            Assert.IsTrue(played);
            Assert.AreEqual(1025, context.MoneyManager.CurrentMoney);
            Assert.AreEqual(0, context.DeckManager.HandCount);
            Assert.IsNull(card.PlacedRoom);
            Assert.AreEqual(0, context.BoardManager.GetAllFieldCards().Count);
        }

        [Test]
        public void PlayCard_EventWithRoomTarget_UsesSelectedRoomWithoutOccupyingRoom()
        {
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(126, "First")),
                CreateLibrary("NormalPool", CreateCardData(127, "Normal")),
                CreateLibrary("RewardPool", CreateCardData(128, "Reward")),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            var room = context.BoardManager.AddRoom(tenantSlots: 1, equipmentSlots: 1);

            var existingTenantData = CreateCardData(129, "Existing Tenant");
            existingTenantData.cardType = CardType.Tenant;
            existingTenantData.targetKind = CardPlayTargetKind.Room;
            existingTenantData.durability = 5;
            var existingTenant = new CardInstance(existingTenantData);
            Assert.IsTrue(room.PlaceCard(existingTenant));

            var roomEvent = CreateCardData(130, "Room Event");
            roomEvent.cardType = CardType.Event;
            roomEvent.targetKind = CardPlayTargetKind.Room;
            roomEvent.instantEffect = "AddTenantDurabilityInSelectedRoom;2";

            context.DeckManager.AddCardToHand(roomEvent);
            var card = context.DeckManager.Hand[0];

            context.TurnManager.StartActionPhase();

            var missingTargetValidation = context.TurnManager.ValidatePlay(card);
            Assert.IsFalse(missingTargetValidation.IsValid);
            Assert.AreEqual(CardPlayBlockReason.MissingTarget, missingTargetValidation.BlockReason);

            bool played = context.TurnManager.PlayCard(card, room);

            Assert.IsTrue(played);
            Assert.AreEqual(7, existingTenant.CurrentDurability);
            Assert.AreEqual(1, room.TenantCount);
            Assert.IsNull(card.PlacedRoom);
            Assert.AreEqual(0, context.DeckManager.HandCount);
        }

        [Test]
        public void PlayCard_RoomEventExpandSlot_ExpandsSelectedRoomCapacity()
        {
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(131, "First")),
                CreateLibrary("NormalPool", CreateCardData(132, "Normal")),
                CreateLibrary("RewardPool", CreateCardData(133, "Reward")),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            var room = context.BoardManager.AddRoom(tenantSlots: 1, equipmentSlots: 1);

            var roomEvent = CreateCardData(134, "Expand Event");
            roomEvent.cardType = CardType.Event;
            roomEvent.targetKind = CardPlayTargetKind.Room;
            roomEvent.instantEffect = "ExpandSlot;1";

            context.DeckManager.AddCardToHand(roomEvent);
            var card = context.DeckManager.Hand[0];

            context.TurnManager.StartActionPhase();

            LogAssert.Expect(LogType.Exception, "System.Exception: SelectedLocale is null. Database could not get table.");
            bool played = context.TurnManager.PlayCard(card, room);

            Assert.IsTrue(played);
            Assert.AreEqual(2, room.TenantSlotCapacity);
            Assert.AreEqual(0, room.TenantCount);
            Assert.IsNull(card.PlacedRoom);
        }

        [Test]
        public void PlayCard_AddRoomEvent_IncreasesBoardRoomCount()
        {
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(135, "First")),
                CreateLibrary("NormalPool", CreateCardData(136, "Normal")),
                CreateLibrary("RewardPool", CreateCardData(137, "Reward")),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            var roomEvent = CreateCardData(138, "Add Room");
            roomEvent.cardType = CardType.Event;
            roomEvent.targetKind = CardPlayTargetKind.PlayArea;
            roomEvent.instantEffect = "AddRoom";

            context.DeckManager.AddCardToHand(roomEvent);
            var card = context.DeckManager.Hand[0];

            context.TurnManager.StartActionPhase();
            int roomCountBefore = context.BoardManager.RoomCount;

            bool played = context.TurnManager.PlayCard(card);

            Assert.IsTrue(played);
            Assert.AreEqual(roomCountBefore + 1, context.BoardManager.RoomCount);
        }

        [Test]
        public void PlayCard_RoomEventAddMoneyBySelectedRoomTenantCount_UsesOnlySelectedRoom()
        {
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(139, "First")),
                CreateLibrary("NormalPool", CreateCardData(140, "Normal")),
                CreateLibrary("RewardPool", CreateCardData(141, "Reward")),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            var targetRoom = context.BoardManager.AddRoom(tenantSlots: 2, equipmentSlots: 1);
            var otherRoom = context.BoardManager.AddRoom(tenantSlots: 1, equipmentSlots: 1);

            Assert.IsTrue(targetRoom.PlaceCard(CreateTenantInstance(142, "Tenant A")));
            Assert.IsTrue(targetRoom.PlaceCard(CreateTenantInstance(143, "Tenant B")));
            Assert.IsTrue(otherRoom.PlaceCard(CreateTenantInstance(144, "Other Tenant")));

            var roomEvent = CreateCardData(145, "Tenant Count Bonus");
            roomEvent.cardType = CardType.Event;
            roomEvent.targetKind = CardPlayTargetKind.Room;
            roomEvent.instantEffect = "AddMoneyBySelectedRoomTenantCount;30";

            context.DeckManager.AddCardToHand(roomEvent);
            var card = context.DeckManager.Hand[0];

            context.TurnManager.StartActionPhase();

            bool played = context.TurnManager.PlayCard(card, targetRoom);

            Assert.IsTrue(played);
            Assert.AreEqual(1060, context.MoneyManager.CurrentMoney);
        }

        [Test]
        public void PlayCard_RoomEventTriggerSelectedRoomSettle_DoesNotReduceDurability_AndSkipsRecursiveExtraSettle()
        {
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(146, "First")),
                CreateLibrary("NormalPool", CreateCardData(147, "Normal")),
                CreateLibrary("RewardPool", CreateCardData(148, "Reward")),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            var room = context.BoardManager.AddRoom(tenantSlots: 1, equipmentSlots: 2);

            var tenantData = CreateCardData(149, "Burst Tenant");
            tenantData.cardType = CardType.Tenant;
            tenantData.targetKind = CardPlayTargetKind.Room;
            tenantData.baseRent = 40;
            tenantData.durability = 5;
            var tenant = new CardInstance(tenantData);

            var recursiveEquipmentData = CreateCardData(150, "Recursive Equipment");
            recursiveEquipmentData.cardType = CardType.Equipment;
            recursiveEquipmentData.targetKind = CardPlayTargetKind.Room;
            recursiveEquipmentData.settleEffect = "TriggerSelectedRoomSettle";
            var recursiveEquipment = new CardInstance(recursiveEquipmentData);

            var supportEquipmentData = CreateCardData(151, "Support Equipment");
            supportEquipmentData.cardType = CardType.Equipment;
            supportEquipmentData.targetKind = CardPlayTargetKind.Room;
            supportEquipmentData.settleEffect = "AddMoney;10";
            var supportEquipment = new CardInstance(supportEquipmentData);

            Assert.IsTrue(room.PlaceCard(tenant));
            Assert.IsTrue(room.PlaceCard(recursiveEquipment));
            Assert.IsTrue(room.PlaceCard(supportEquipment));

            var roomEvent = CreateCardData(152, "Burst Trigger");
            roomEvent.cardType = CardType.Event;
            roomEvent.targetKind = CardPlayTargetKind.Room;
            roomEvent.instantEffect = "TriggerSelectedRoomSettle";

            context.DeckManager.AddCardToHand(roomEvent);
            var card = context.DeckManager.Hand[0];

            context.TurnManager.StartActionPhase();

            bool played = context.TurnManager.PlayCard(card, room);

            Assert.IsTrue(played);
            Assert.AreEqual(1050, context.MoneyManager.CurrentMoney);
            Assert.AreEqual(5, tenant.CurrentDurability);
            Assert.IsFalse(context.GameManager.GameContext.EffectContext.IsExtraRoomSettlementActive);
        }

        [Test]
        public void PlayCard_RoomTenant_ResolvesInstantEffectAfterPlacement()
        {
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(153, "First")),
                CreateLibrary("NormalPool", CreateCardData(154, "Normal")),
                CreateLibrary("RewardPool", CreateCardData(155, "Reward")),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            var room = context.BoardManager.AddRoom(tenantSlots: 1, equipmentSlots: 1);
            var tenantData = CreateCardData(156, "Instant Tenant");
            tenantData.cardType = CardType.Tenant;
            tenantData.targetKind = CardPlayTargetKind.Room;
            tenantData.cost = 50;
            tenantData.instantEffect = "AddMoney;60";

            context.DeckManager.AddCardToHand(tenantData);
            var card = context.DeckManager.Hand[0];

            context.TurnManager.StartActionPhase();

            bool played = context.TurnManager.PlayCard(card, room);

            Assert.IsTrue(played);
            Assert.AreEqual(1010, context.MoneyManager.CurrentMoney);
            Assert.AreSame(room, card.PlacedRoom);
            Assert.AreEqual(1, room.TenantCount);
        }

        [Test]
        public void ExecuteSettlePhase_FieldCardWaitTurns_DoNotExpireOrTriggerDestroyEffect()
        {
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(157, "First")),
                CreateLibrary("NormalPool", CreateCardData(158, "Normal")),
                CreateLibrary("RewardPool"),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);
            context.GameManager.gameConfig.loanInterval = 0;
            SetPrivateField(context.TurnManager, "_currentTurn", 1);

            var room = context.BoardManager.AddRoom(tenantSlots: 1, equipmentSlots: 1);
            var tenantData = CreateCardData(159, "Lease End Bonus");
            tenantData.cardType = CardType.Tenant;
            tenantData.targetKind = CardPlayTargetKind.Room;
            tenantData.waitTurns = 1;
            tenantData.durability = 0;
            tenantData.destroyEffect = "AddMoney;70";
            var tenant = new CardInstance(tenantData);
            Assert.IsTrue(room.PlaceCard(tenant));

            GameEvents.CardDestroyed? destroyed = null;
            void OnCardDestroyed(GameEvents.CardDestroyed e) => destroyed = e;
            EventBus.Subscribe<GameEvents.CardDestroyed>(OnCardDestroyed);

            try
            {
                context.TurnManager.ExecuteSettlePhase();
            }
            finally
            {
                EventBus.Unsubscribe<GameEvents.CardDestroyed>(OnCardDestroyed);
            }

            Assert.AreEqual(1000, context.MoneyManager.CurrentMoney);
            Assert.IsFalse(tenant.IsDestroyed);
            Assert.AreEqual(1, tenant.CurrentWait);
            Assert.AreEqual(1, room.TenantCount);
            Assert.IsFalse(destroyed.HasValue);
        }

        [Test]
        public void ResolveHandWaitAndDiscardExpired_RemovesExpiredWaitingCardWithoutDestroyOrDiscard()
        {
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(160, "First")),
                CreateLibrary("NormalPool", CreateCardData(161, "Normal")),
                CreateLibrary("RewardPool", CreateCardData(162, "Reward")),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            var waitingCardData = CreateCardData(163, "Waiting Tenant");
            waitingCardData.cardType = CardType.Tenant;
            waitingCardData.waitTurns = 1;
            waitingCardData.destroyEffect = "AddMoney;70";
            var waitingCard = context.DeckManager.AddCardToHand(waitingCardData);

            int removed = context.DeckManager.ResolveHandWaitAndDiscardExpired();

            Assert.AreEqual(1, removed);
            Assert.AreEqual(0, context.DeckManager.HandCount);
            Assert.AreEqual(0, context.DeckManager.DiscardPileCount);
            Assert.IsFalse(waitingCard.IsDestroyed);
            Assert.AreEqual(1000, context.MoneyManager.CurrentMoney);
        }

        [Test]
        public void GameOverPanel_HidesRootOnAwake_AndShowsRootAndPanel()
        {
            var root = CreateGameObject("GameOverPanel");
            var panel = CreateGameObject("Panel");
            panel.transform.SetParent(root.transform);

            var gameOverPanel = root.AddComponent<UIGameOverPanel>();
            gameOverPanel.panel = panel;

            gameOverPanel.Hide();
            Assert.IsFalse(root.activeSelf);
            Assert.IsFalse(panel.activeSelf);

            gameOverPanel.Show(3, 20);

            Assert.IsTrue(root.activeSelf);
            Assert.IsTrue(panel.activeSelf);
        }

        [Test]
        public void InitializeSystems_ThrowsWhenBoardManagerIsMissing()
        {
            CreateComponent<MoneyManager>("MoneyManager");
            CreateComponent<DeckManager>("DeckManager");

            var gameConfig = CreateGameConfig(
                CreateLibrary("FirstPool", CreateCardData(80, "First")),
                CreateLibrary("NormalPool", CreateCardData(81, "Normal")),
                CreateLibrary("RewardPool", CreateCardData(82, "Reward")),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            var gameManager = CreateComponent<GameManager>("GameManager");
            gameManager.gameConfig = gameConfig;

            LogAssert.Expect(LogType.Error, "[Singleton] No instance of BaoZuPo.Board.BoardManager found in scene!");
            var exception = Assert.Throws<InvalidOperationException>(() => InvokePrivateMethod(gameManager, "InitializeSystems"));
            StringAssert.Contains("BoardManager", exception.Message);
        }

        [Test]
        public void InitializeSystems_ThrowsWhenDeckManagerIsMissing()
        {
            var roomsRoot = CreateGameObject("RoomsRoot").transform;
            var boardManager = CreateComponent<BoardManager>("BoardManager");
            SetPrivateField(boardManager, "_roomRoot", roomsRoot);
            CreateComponent<MoneyManager>("MoneyManager");

            var gameConfig = CreateGameConfig(
                CreateLibrary("FirstPool", CreateCardData(90, "First")),
                CreateLibrary("NormalPool", CreateCardData(91, "Normal")),
                CreateLibrary("RewardPool", CreateCardData(92, "Reward")),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            var gameManager = CreateComponent<GameManager>("GameManager");
            gameManager.gameConfig = gameConfig;

            LogAssert.Expect(LogType.Error, "[Singleton] No instance of BaoZuPo.Deck.DeckManager found in scene!");
            var exception = Assert.Throws<InvalidOperationException>(() => InvokePrivateMethod(gameManager, "InitializeSystems"));
            StringAssert.Contains("DeckManager", exception.Message);
        }

        private GameplayContext CreateGameplayContext(
            CardLibrary firstTurnLibrary,
            CardLibrary normalTurnLibrary,
            CardLibrary rewardLibrary,
            int firstTurnDrawCount,
            int normalTurnDrawCount,
            int maxHandSize)
        {
            var roomsRoot = CreateGameObject("RoomsRoot").transform;
            var boardManager = CreateComponent<BoardManager>("BoardManager");
            SetPrivateField(boardManager, "_roomRoot", roomsRoot);

            var moneyManager = CreateComponent<MoneyManager>("MoneyManager");
            var deckManager = CreateComponent<DeckManager>("DeckManager");
            var turnManager = CreateComponent<TurnManager>("TurnManager");
            CreateComponent<UIManager>("UIManager");

            var gameConfig = CreateGameConfig(
                firstTurnLibrary,
                normalTurnLibrary,
                rewardLibrary,
                firstTurnDrawCount,
                normalTurnDrawCount,
                maxHandSize);

            var gameManager = CreateComponent<GameManager>("GameManager");
            gameManager.gameConfig = gameConfig;
            InvokePrivateMethod(gameManager, "InitializeSystems");

            return new GameplayContext(gameManager, turnManager, deckManager, boardManager, moneyManager);
        }

        private GameConfig CreateGameConfig(
            CardLibrary firstTurnLibrary,
            CardLibrary normalTurnLibrary,
            CardLibrary rewardLibrary,
            int firstTurnDrawCount,
            int normalTurnDrawCount,
            int maxHandSize)
        {
            var gameConfig = CreateScriptableObject<GameConfig>();
            gameConfig.startingMoney = 1000;
            gameConfig.firstTurnDrawCount = firstTurnDrawCount;
            gameConfig.normalTurnDrawCount = normalTurnDrawCount;
            gameConfig.maxHandSize = maxHandSize;
            gameConfig.initialRoomCount = 0;
            gameConfig.defaultTenantSlots = 1;
            gameConfig.defaultEquipmentSlots = 1;
            gameConfig.firstTurnDrawLibrary = firstTurnLibrary;
            gameConfig.normalTurnDrawLibrary = normalTurnLibrary;
            gameConfig.rewardLibrary = rewardLibrary;
            return gameConfig;
        }

        private T CreateComponent<T>(string name) where T : Component
        {
            return CreateGameObject(name).AddComponent<T>();
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private T CreateScriptableObject<T>() where T : ScriptableObject
        {
            var scriptableObject = ScriptableObject.CreateInstance<T>();
            _createdObjects.Add(scriptableObject);
            return scriptableObject;
        }

        private CardLibrary CreateLibrary(string libraryId, params CardData[] cards)
        {
            var library = CreateScriptableObject<CardLibrary>();
            library.libraryId = libraryId;
            library.displayName = libraryId;
            library.cards = new List<CardData>(cards);
            return library;
        }

        private CardData CreateCardData(int cardId, string cardName = null)
        {
            var card = CreateScriptableObject<CardData>();
            card.cardId = cardId;
            card.cardName = cardName ?? $"Card {cardId}";
            card.cardType = CardType.Event;
            card.rarity = CardRarity.Common;
            card.cost = 0;
            card.baseRent = 0;
            card.waitTurns = 0;
            card.durability = 0;
            return card;
        }

        private CardInstance CreateTenantInstance(int cardId, string cardName)
        {
            var tenantData = CreateCardData(cardId, cardName);
            tenantData.cardType = CardType.Tenant;
            tenantData.targetKind = CardPlayTargetKind.Room;
            tenantData.durability = 5;
            return new CardInstance(tenantData);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Missing private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"Missing private method '{methodName}' on {target.GetType().Name}.");
            try
            {
                method.Invoke(target, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private sealed class GameplayContext
        {
            public GameplayContext(
                GameManager gameManager,
                TurnManager turnManager,
                DeckManager deckManager,
                BoardManager boardManager,
                MoneyManager moneyManager)
            {
                GameManager = gameManager;
                TurnManager = turnManager;
                DeckManager = deckManager;
                BoardManager = boardManager;
                MoneyManager = moneyManager;
            }

            public GameManager GameManager { get; }
            public TurnManager TurnManager { get; }
            public DeckManager DeckManager { get; }
            public BoardManager BoardManager { get; }
            public MoneyManager MoneyManager { get; }
        }
    }
}

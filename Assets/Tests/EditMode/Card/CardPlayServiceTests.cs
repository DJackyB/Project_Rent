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

namespace BaoZuPo.Tests.Card
{
    public sealed class CardPlayServiceTests
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
        public void ValidatePlay_ReturnsExpectedBlockReasons()
        {
            var context = CreateGameplayContext();
            var service = new CardPlayService();
            var eventCard = new CardInstance(CreateEventCard(101, "Event"));

            Assert.AreEqual(
                CardPlayBlockReason.NotActionPhase,
                service.ValidatePlay(eventCard).BlockReason);

            context.TurnManager.StartActionPhase();

            var expensiveCard = new CardInstance(CreateEventCard(102, "Expensive", cost: 1001));
            Assert.AreEqual(
                CardPlayBlockReason.InsufficientMoney,
                service.ValidatePlay(expensiveCard).BlockReason);

            var roomCard = new CardInstance(CreateTenantCard(103, "Tenant"));
            Assert.AreEqual(
                CardPlayBlockReason.MissingTarget,
                service.ValidatePlay(roomCard).BlockReason);

            var fullRoom = context.BoardManager.AddRoom(tenantSlots: 1, equipmentSlots: 1);
            Assert.IsTrue(fullRoom.PlaceCard(new CardInstance(CreateTenantCard(104, "Occupant"))));

            Assert.AreEqual(
                CardPlayBlockReason.TargetFull,
                service.ValidatePlay(roomCard, fullRoom).BlockReason);
        }

        [Test]
        public void PlayCard_EventCard_PaysCost_ExecutesInstantEffect_RemovesFromHand_AndDiscards()
        {
            var context = CreateGameplayContext();
            var service = new CardPlayService();
            var cardData = CreateEventCard(201, "Cash Event", cost: 100, instantEffect: "AddMoney;25");
            var card = context.DeckManager.ForceAddCardToHand(cardData);
            GameEvents.CardPlayed? playedEvent = null;

            void OnCardPlayed(GameEvents.CardPlayed evt) => playedEvent = evt;
            EventBus.Subscribe<GameEvents.CardPlayed>(OnCardPlayed);

            try
            {
                context.TurnManager.StartActionPhase();
                var result = service.Play(card);

                Assert.IsTrue(result.Succeeded);
                Assert.AreEqual(925, context.MoneyManager.CurrentMoney);
                Assert.AreEqual(0, context.DeckManager.HandCount);
                Assert.AreEqual(1, context.DeckManager.DiscardPileCount);
                Assert.AreSame(card, context.DeckManager.DiscardPile[0]);
                Assert.IsTrue(playedEvent.HasValue);
                Assert.AreSame(card, playedEvent.Value.Card);
            }
            finally
            {
                EventBus.Unsubscribe<GameEvents.CardPlayed>(OnCardPlayed);
            }
        }

        [Test]
        public void PlayCard_RoomTenant_PlacesInRoom_PaysCost_ExecutesInstantEffect_AndDoesNotDiscard()
        {
            var context = CreateGameplayContext();
            var service = new CardPlayService();
            var room = context.BoardManager.AddRoom(tenantSlots: 1, equipmentSlots: 1);
            var cardData = CreateTenantCard(301, "Tenant", cost: 100, instantEffect: "AddMoney;25");
            var card = context.DeckManager.ForceAddCardToHand(cardData);

            context.TurnManager.StartActionPhase();
            var result = service.Play(card, room);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(925, context.MoneyManager.CurrentMoney);
            Assert.AreEqual(0, context.DeckManager.HandCount);
            Assert.AreEqual(0, context.DeckManager.DiscardPileCount);
            Assert.AreSame(card, room.GetTenants()[0]);
            Assert.AreSame(room, card.PlacedRoom);
        }

        [Test]
        public void PlayCard_Contract_AddsContract_PaysCost_AndDoesNotDiscard()
        {
            var context = CreateGameplayContext();
            var service = new CardPlayService();
            var cardData = CreateContractCard(401, "Contract", cost: 100);
            var card = context.DeckManager.ForceAddCardToHand(cardData);

            context.TurnManager.StartActionPhase();
            var result = service.Play(card);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(900, context.MoneyManager.CurrentMoney);
            Assert.AreEqual(0, context.DeckManager.HandCount);
            Assert.AreEqual(0, context.DeckManager.DiscardPileCount);
            Assert.AreEqual(1, context.BoardManager.ContractCount);
            Assert.AreSame(card, context.BoardManager.GetAllContracts()[0]);
        }

        private GameplayContext CreateGameplayContext()
        {
            var roomsRoot = CreateGameObject("RoomsRoot").transform;
            var boardManager = CreateComponent<BoardManager>("BoardManager");
            SetPrivateField(boardManager, "_roomRoot", roomsRoot);

            var moneyManager = CreateComponent<MoneyManager>("MoneyManager");
            var deckManager = CreateComponent<DeckManager>("DeckManager");
            var turnManager = CreateComponent<TurnManager>("TurnManager");
            CreateComponent<UIManager>("UIManager");

            var gameConfig = CreateGameConfig();
            var gameManager = CreateComponent<GameManager>("GameManager");
            gameManager.gameConfig = gameConfig;
            InvokePrivateMethod(gameManager, "InitializeSystems");

            return new GameplayContext(gameManager, turnManager, deckManager, boardManager, moneyManager);
        }

        private GameConfig CreateGameConfig()
        {
            var gameConfig = CreateScriptableObject<GameConfig>();
            gameConfig.firstTurnDrawLibrary = CreateLibrary("FirstPool");
            gameConfig.normalTurnDrawLibrary = CreateLibrary("NormalPool");
            gameConfig.rewardLibrary = CreateLibrary("RewardPool");
            gameConfig.shopLibrary = CreateLibrary(
                "ShopPool",
                CreateEventCard(9002, "Shop Offer A"),
                CreateEventCard(9003, "Shop Offer B"),
                CreateEventCard(9004, "Shop Offer C"));
            gameConfig.shopCard = CreateShopCard();
            gameConfig.shopOfferCount = GameConfig.MaxShopOfferCount;
            gameConfig.postSettlementRandomEventChance = 0f;
            gameConfig.postSettlementRandomEventLibraryId = string.Empty;
            gameConfig.startingMoney = 1000;
            gameConfig.firstTurnDrawCount = 0;
            gameConfig.normalTurnDrawCount = 0;
            gameConfig.maxHandSize = 5;
            gameConfig.initialRoomCount = 0;
            gameConfig.defaultTenantSlots = 1;
            gameConfig.defaultEquipmentSlots = 1;
            return gameConfig;
        }

        private CardData CreateEventCard(
            int cardId,
            string cardName,
            int cost = 0,
            string instantEffect = null)
        {
            var card = CreateBaseCard(cardId, cardName, cost, instantEffect);
            card.cardType = CardType.Event;
            card.targetKind = CardPlayTargetKind.PlayArea;
            return card;
        }

        private CardData CreateTenantCard(
            int cardId,
            string cardName,
            int cost = 0,
            string instantEffect = null)
        {
            var card = CreateBaseCard(cardId, cardName, cost, instantEffect);
            card.cardType = CardType.Tenant;
            card.targetKind = CardPlayTargetKind.Room;
            card.durability = 5;
            return card;
        }

        private CardData CreateContractCard(int cardId, string cardName, int cost = 0)
        {
            var card = CreateBaseCard(cardId, cardName, cost, null);
            card.cardType = CardType.Contract;
            card.targetKind = CardPlayTargetKind.PlayArea;
            card.durability = 5;
            return card;
        }

        private CardData CreateShopCard()
        {
            return CreateEventCard(9001, "Shop", instantEffect: "OpenShop");
        }

        private CardData CreateBaseCard(
            int cardId,
            string cardName,
            int cost,
            string instantEffect)
        {
            var card = CreateScriptableObject<CardData>();
            card.cardId = cardId;
            card.cardName = cardName;
            card.cardType = CardType.Event;
            card.rarity = CardRarity.Common;
            card.cost = cost;
            card.baseRent = 0;
            card.waitTurns = 0;
            card.durability = 0;
            card.instantEffect = instantEffect;
            return card;
        }

        private CardLibrary CreateLibrary(string libraryId, params CardData[] cards)
        {
            var library = CreateScriptableObject<CardLibrary>();
            library.libraryId = libraryId;
            library.displayName = libraryId;
            library.entries = cards.Select(card => new CardLibraryEntry { card = card, quantity = 1 }).ToList();
            return library;
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

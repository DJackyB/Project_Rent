using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Deck;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
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
            CardLibraryDatabase.Clear();
            CardEffectFactory.ClearAll();
            CardEffectRegistration.EnsureRegistered();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ClearAll();
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
            Assert.Greater(library.cards.Count, 0);
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

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
        public void RewardGeneration_OffersConfiguredRewardLibraryOptions()
        {
            var rewardCard = CreateCardData(40, "Reward");
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(41, "First")),
                CreateLibrary("NormalPool", CreateCardData(42, "Normal")),
                CreateLibrary("RewardPool", rewardCard),
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
            foreach (var option in offered.Value.Options)
            {
                Assert.AreSame(rewardCard, option);
            }
            Assert.AreEqual(0, context.DeckManager.HandCount);
            Assert.IsTrue(context.TurnManager.IsRewardSelectionPending);
        }

        [Test]
        public void RewardSelection_AddsChosenCardToHand_AndClearsPending()
        {
            var rewardCard = CreateCardData(50, "Reward");
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(51, "First")),
                CreateLibrary("NormalPool", CreateCardData(52, "Normal")),
                CreateLibrary("RewardPool", rewardCard),
                firstTurnDrawCount: 0,
                normalTurnDrawCount: 0,
                maxHandSize: 5);

            InvokePrivateMethod(context.TurnManager, "AwardOneCardFromThreeOptions", false);
            EventBus.Publish(new GameEvents.CardRewardSelected { ChosenCard = rewardCard });

            Assert.AreEqual(1, context.DeckManager.HandCount);
            Assert.AreSame(rewardCard, context.DeckManager.Hand[0].Data);
            Assert.IsFalse(context.TurnManager.IsRewardSelectionPending);
        }

        [Test]
        public void RewardSelection_SkipClearsPending_WithoutAddingCard()
        {
            var rewardCard = CreateCardData(60, "Reward");
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(61, "First")),
                CreateLibrary("NormalPool", CreateCardData(62, "Normal")),
                CreateLibrary("RewardPool", rewardCard),
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
            var commonReward = CreateCardData(70, "Common Reward");
            var context = CreateGameplayContext(
                CreateLibrary("FirstPool", CreateCardData(71, "First")),
                CreateLibrary("NormalPool", CreateCardData(72, "Normal")),
                CreateLibrary("RewardPool", commonReward),
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
            foreach (var option in offered.Value.Options)
            {
                Assert.AreSame(commonReward, option);
            }
            Assert.IsTrue(offered.Value.Boosted);
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

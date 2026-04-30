using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Deck;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using BaoZuPo.UI;
using Cysharp.Threading.Tasks;
using Martian.EventBus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BaoZuPo.Tests.Card
{
    public sealed class TurnFlowServiceTests
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
        public void EnsureInitialized_ThrowsWhenGameConfigIsMissing()
        {
            var gameManager = CreateComponent<GameManager>("GameManager");

            var exception = Assert.Throws<InvalidOperationException>(() => gameManager.EnsureInitialized());
            StringAssert.Contains("GameConfig is not assigned", exception.Message);
        }

        [UnityTest]
        public IEnumerator RunAsync_DrivesTurnLoopThroughSettlementAndRewardSkip()
        {
            var context = CreateGameplayContext();
            var service = new TurnFlowService(context.TurnManager);
            var cancellation = new CancellationTokenSource();

            int turnEndedCount = 0;
            bool rewardOffered = false;
            void OnTurnEnded(GameEvents.TurnEnded _) => turnEndedCount++;
            void OnRewardOffered(GameEvents.CardRewardOffered _) => rewardOffered = true;

            EventBus.Subscribe<GameEvents.TurnEnded>(OnTurnEnded);
            EventBus.Subscribe<GameEvents.CardRewardOffered>(OnRewardOffered);

            var flowTask = service.RunAsync(cancellation.Token).SuppressCancellationThrow();
            bool wasCanceled = false;

            try
            {
                yield return WaitUntilOrFail(
                    () => context.TurnManager.CurrentTurn == 1
                        && context.TurnManager.CurrentPhase == GamePhase.Action,
                    "TurnFlowService did not reach first action phase.");

                context.TurnManager.EndActionPhase();

                yield return WaitUntilOrFail(
                    () => rewardOffered,
                    "TurnFlowService did not enter reward flow after settlement.");

                EventBus.Publish(new GameEvents.CardRewardSelected { ChosenCard = null });

                yield return WaitUntilOrFail(
                    () => turnEndedCount == 1,
                    "TurnFlowService did not complete the first turn after reward skip.");

                yield return WaitUntilOrFail(
                    () => context.TurnManager.CurrentTurn == 2
                        && context.TurnManager.CurrentPhase == GamePhase.Action,
                    "TurnFlowService did not start the next turn.");
            }
            finally
            {
                EventBus.Unsubscribe<GameEvents.TurnEnded>(OnTurnEnded);
                EventBus.Unsubscribe<GameEvents.CardRewardOffered>(OnRewardOffered);
                cancellation.Cancel();
            }

            yield return flowTask.ToCoroutine(canceled => wasCanceled = canceled);
            cancellation.Dispose();
            Assert.IsTrue(wasCanceled);
        }

        private static IEnumerator WaitUntilOrFail(Func<bool> predicate, string message, int maxFrames = 180)
        {
            for (int i = 0; i < maxFrames; i++)
            {
                if (predicate())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail(message);
        }

        private GameplayContext CreateGameplayContext()
        {
            var roomsRoot = CreateGameObject("RoomsRoot").transform;
            var boardManager = CreateComponent<BoardManager>("BoardManager");
            SetPrivateField(boardManager, "_roomRoot", roomsRoot);

            var moneyManager = CreateComponent<MoneyManager>("MoneyManager");
            var deckManager = CreateComponent<DeckManager>("DeckManager");
            var turnManager = CreateComponent<TurnManager>("TurnManager");

            var uiManager = CreateComponent<UIManager>("UIManager");
            uiManager.enabled = false;

            var gameConfig = CreateGameConfig();
            var gameManager = CreateComponent<GameManager>("GameManager");
            gameManager.gameConfig = gameConfig;
            InvokePrivateMethod(gameManager, "InitializeSystems");

            turnManager.Construct(
                new CardPlayService(),
                new SettlementService(),
                new SettlementPresentationMapper(),
                new SettlementPresentationService(),
                new RewardService(),
                new ShopService());

            return new GameplayContext(gameManager, turnManager, deckManager, boardManager, moneyManager);
        }

        private GameConfig CreateGameConfig()
        {
            var gameConfig = CreateScriptableObject<GameConfig>();
            gameConfig.firstTurnDrawLibrary = CreateLibrary("FirstPool");
            gameConfig.normalTurnDrawLibrary = CreateLibrary("NormalPool");
            gameConfig.rewardLibrary = CreateLibrary("RewardPool", CreateEventCard(1001, "Reward"));
            gameConfig.shopLibrary = CreateLibrary(
                "ShopPool",
                CreateEventCard(1002, "Shop Offer A"),
                CreateEventCard(1003, "Shop Offer B"),
                CreateEventCard(1004, "Shop Offer C"));
            gameConfig.shopCard = CreateEventCard(1005, "Shop", instantEffect: "OpenShop");
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
            gameConfig.loanInterval = 0;
            return gameConfig;
        }

        private CardData CreateEventCard(int cardId, string cardName, string instantEffect = null)
        {
            var card = CreateScriptableObject<CardData>();
            card.cardId = cardId;
            card.cardName = cardName;
            card.cardType = CardType.Event;
            card.targetKind = CardPlayTargetKind.PlayArea;
            card.rarity = CardRarity.Common;
            card.cost = 0;
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

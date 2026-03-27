using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Deck;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using BaoZuPo.Save;
using BaoZuPo.UI;
using Martian.Save;
using Martian.Save.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace BaoZuPo.Tests.Save
{
    public class BaoZuPoSaveIntegrationTests
    {
        private const string LegacyLanguageKey = "BaoZuPo.Language";

        private readonly List<UnityEngine.Object> _ownedObjects = new();
        private string _tempDirectory;
        private GameplaySaveFacade _gameplaySaveFacade;
        private SettingsSaveFacade _settingsSaveFacade;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "BaoZuPoSaveTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            var saveService = new SaveService(
                new UnityJsonSaveSerializer(),
                new PersistentDataPathSaveStorage(_tempDirectory),
                new SaveRuntimeOptions());

            _gameplaySaveFacade = new GameplaySaveFacade(saveService);
            _settingsSaveFacade = new SettingsSaveFacade(saveService, "settings/app-settings.json");

            GameplaySaveFacade.SetSharedInstance(_gameplaySaveFacade);
            SettingsSaveFacade.SetSharedInstance(_settingsSaveFacade);

            ResetLocalizationStatics();
            PlayerPrefs.DeleteKey(LegacyLanguageKey);
            PlayerPrefs.Save();

            CardDatabase.Clear();
            CreateTestCards();
            CreateManagers();
        }

        [TearDown]
        public void TearDown()
        {
            GameplaySaveFacade.SetSharedInstance(null);
            SettingsSaveFacade.SetSharedInstance(null);
            ResetLocalizationStatics();
            CardDatabase.Clear();
            PlayerPrefs.DeleteKey(LegacyLanguageKey);
            PlayerPrefs.Save();

            for (int i = _ownedObjects.Count - 1; i >= 0; i--)
            {
                if (_ownedObjects[i] == null)
                {
                    continue;
                }

                if (_ownedObjects[i] is GameObject gameObject)
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_ownedObjects[i]);
                }
            }

            _ownedObjects.Clear();

            if (!string.IsNullOrWhiteSpace(_tempDirectory) && Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Test]
        public void GameplaySave_RoundTripsMoneyBoardDeckAndTurnState()
        {
            ArrangeGameplayState();

            Assert.IsTrue(_gameplaySaveFacade.Save("slot-a", out var saveError), saveError);

            MoneyManager.Instance.RestoreState(new EconomySaveState { currentMoney = 12, totalSpent = 1 });
            BoardManager.Instance.Initialize(1, 1, 1);
            DeckManager.Instance.RestoreState(new DeckSaveState { maxHandSize = 3 });
            TurnManager.Instance.RestoreState(new TurnSaveState
            {
                currentTurn = 1,
                loanPaymentCount = 0,
                currentPhase = GamePhase.Action,
                actionPhaseEnded = false,
                isGameOver = false
            });

            Assert.IsTrue(_gameplaySaveFacade.Load("slot-a", out var loadError), loadError);

            Assert.AreEqual(321, MoneyManager.Instance.CurrentMoney);
            Assert.AreEqual(45, MoneyManager.Instance.TotalSpent);
            Assert.AreEqual(2, BoardManager.Instance.RoomCount);
            Assert.AreEqual(1, BoardManager.Instance.GetRoom(0).TenantCount);
            Assert.AreEqual(1, BoardManager.Instance.GetRoom(0).EquipmentCount);
            Assert.AreEqual(1, BoardManager.Instance.ContractCount);
            Assert.AreEqual(5, DeckManager.Instance.MaxHandSize);
            Assert.AreEqual(2, DeckManager.Instance.DrawPileCount);
            Assert.AreEqual(2, DeckManager.Instance.HandCount);
            Assert.AreEqual(1, DeckManager.Instance.DiscardPileCount);
            Assert.AreEqual(201, DeckManager.Instance.Hand[0].Data.cardId);
            Assert.AreEqual(301, DeckManager.Instance.Hand[1].Data.cardId);
            Assert.AreEqual(6, TurnManager.Instance.CurrentTurn);
            Assert.AreEqual(2, TurnManager.Instance.CaptureState().loanPaymentCount);
            Assert.AreEqual(GamePhase.Action, TurnManager.Instance.CurrentPhase);
            Assert.IsFalse(TurnManager.Instance.IsGameOver);
        }

        [Test]
        public void GameplayLoad_FailsOnUnknownCardWithoutMutatingRuntimeState()
        {
            ArrangeGameplayState();
            Assert.IsTrue(_gameplaySaveFacade.Save("slot-b", out var saveError), saveError);

            var savePath = Path.Combine(_tempDirectory, "gameplay", "slot-b.json");
            var saveJson = File.ReadAllText(savePath);
            saveJson = saveJson.Replace("\"cardId\":101", "\"cardId\":9999");
            File.WriteAllText(savePath, saveJson);

            MoneyManager.Instance.RestoreState(new EconomySaveState { currentMoney = 777, totalSpent = 2 });
            BoardManager.Instance.Initialize(1, 1, 1);

            Assert.IsFalse(_gameplaySaveFacade.Load("slot-b", out var loadError));
            StringAssert.Contains("Unknown cardId", loadError);
            Assert.AreEqual(777, MoneyManager.Instance.CurrentMoney);
            Assert.AreEqual(1, BoardManager.Instance.RoomCount);
        }

        [Test]
        public void SettingsLoad_FallsBackToLegacyPlayerPrefs_AndSaveWritesSettingsFile()
        {
            PlayerPrefs.SetInt(LegacyLanguageKey, (int)AppLanguage.English);
            PlayerPrefs.Save();
            ResetLocalizationStatics();

            Assert.AreEqual(AppLanguage.English, LocalizationManager.CurrentLanguage);

            LocalizationManager.SetLanguage(AppLanguage.Chinese);

            var settingsPath = Path.Combine(_tempDirectory, "settings", "app-settings.json");
            Assert.IsTrue(File.Exists(settingsPath));

            PlayerPrefs.SetInt(LegacyLanguageKey, (int)AppLanguage.English);
            PlayerPrefs.Save();
            ResetLocalizationStatics();

            Assert.IsTrue(_settingsSaveFacade.Load(out var loadError), loadError);
            Assert.AreEqual(AppLanguage.Chinese, LocalizationManager.CurrentLanguage);
        }

        private void ArrangeGameplayState()
        {
            MoneyManager.Instance.RestoreState(new EconomySaveState
            {
                currentMoney = 321,
                totalSpent = 45
            });

            BoardManager.Instance.Initialize(2, 1, 2);
            Assert.IsTrue(BoardManager.Instance.GetRoom(0).PlaceCard(new CardInstance(CardDatabase.GetById(101))));
            Assert.IsTrue(BoardManager.Instance.GetRoom(0).PlaceCard(new CardInstance(CardDatabase.GetById(201))));
            BoardManager.Instance.AddContract(new CardInstance(CardDatabase.GetById(401)));

            DeckManager.Instance.RestoreState(new DeckSaveState
            {
                maxHandSize = 5,
                drawPile = new List<CardRuntimeState>
                {
                    new() { cardId = 101, currentDurability = 3, currentWait = 2 },
                    new() { cardId = 401, currentDurability = 5, currentWait = 0 }
                },
                hand = new List<CardRuntimeState>
                {
                    new() { cardId = 201, currentDurability = 2, currentWait = 1 },
                    new() { cardId = 301, currentDurability = 0, currentWait = 0 }
                },
                discardPile = new List<CardRuntimeState>
                {
                    new() { cardId = 401, currentDurability = 5, currentWait = 0 }
                }
            });

            TurnManager.Instance.RestoreState(new TurnSaveState
            {
                currentTurn = 6,
                loanPaymentCount = 2,
                currentPhase = GamePhase.Action,
                actionPhaseEnded = false,
                isGameOver = false
            });
        }

        private void CreateManagers()
        {
            CreateComponent<MoneyManager>("MoneyManager");
            var boardManager = CreateComponent<BoardManager>("BoardManager");
            var roomRoot = CreateGameObject("Rooms").transform;
            typeof(BoardManager).GetField("_roomRoot", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(boardManager, roomRoot);

            CreateComponent<DeckManager>("DeckManager");
            CreateComponent<TurnManager>("TurnManager");
        }

        private void CreateTestCards()
        {
            RegisterCard(101, "Tenant", CardType.Tenant, durability: 3, waitTurns: 2);
            RegisterCard(201, "Equipment", CardType.Equipment, durability: 2, waitTurns: 1);
            RegisterCard(301, "Event", CardType.Event, durability: 0, waitTurns: 0);
            RegisterCard(401, "Contract", CardType.Contract, durability: 5, waitTurns: 0);
        }

        private void RegisterCard(int cardId, string cardName, CardType cardType, int durability, int waitTurns)
        {
            var cardData = ScriptableObject.CreateInstance<CardData>();
            cardData.cardId = cardId;
            cardData.cardName = cardName;
            cardData.cardType = cardType;
            cardData.durability = durability;
            cardData.waitTurns = waitTurns;
            cardData.cost = 1;
            cardData.baseRent = 1;
            _ownedObjects.Add(cardData);
            CardDatabase.Register(cardData);
        }

        private T CreateComponent<T>(string name) where T : Component
        {
            var gameObject = CreateGameObject(name);
            return gameObject.AddComponent<T>();
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _ownedObjects.Add(gameObject);
            return gameObject;
        }

        private static void ResetLocalizationStatics()
        {
            typeof(LocalizationManager).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, false);
            typeof(LocalizationManager).GetField("_currentLanguage", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, default(AppLanguage));
        }
    }
}

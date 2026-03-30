using BaoZuPo.Card;
using BaoZuPo.GameFlow;
using Martian.EventBus;
using NodeCanvas.StateMachines;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Core
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("Config")]
        public GameConfig gameConfig;

        private FSMOwner _turnFlowFsm;

        public GameContext GameContext { get; private set; }

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.GameOver>(OnGameOver);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.GameOver>(OnGameOver);
        }

        protected override void Awake()
        {
            base.Awake();
            _turnFlowFsm = GetComponent<FSMOwner>();

            if (gameConfig == null)
            {
                Debug.LogError("[GameManager] GameConfig is not assigned.");
                return;
            }

            InitializeSystems();
        }

        private void InitializeSystems()
        {
            CardEffectRegistration.EnsureRegistered();

            CardDatabase.LoadAll();
            CardLibraryDatabase.LoadAll();
            ValidateConfiguredLibraries();
            ValidateLoadedCardEffects();

            Debug.Log($"[GameManager] Config loaded. Money={gameConfig.startingMoney}, Rooms={gameConfig.initialRoomCount}, LoanGrowth={gameConfig.loanGrowthFactor}");
            Economy.MoneyManager.Instance.Initialize(gameConfig.startingMoney);

            if (Board.BoardManager.Instance != null)
            {
                Board.BoardManager.Instance.Initialize(
                    gameConfig.initialRoomCount,
                    gameConfig.defaultTenantSlots,
                    gameConfig.defaultEquipmentSlots
                );
            }

            if (Deck.DeckManager.Instance != null)
            {
                Deck.DeckManager.Instance.Initialize(gameConfig.maxHandSize);
            }

            GameContext = new GameContext
            {
                MoneyManager = Economy.MoneyManager.Instance,
                BoardManager = Board.BoardManager.Instance,
                DeckManager = Deck.DeckManager.Instance,
            };

            Debug.Log("[GameManager] All systems initialized.");
        }

        private void ValidateConfiguredLibraries()
        {
            var errors = new List<string>();

            ValidateConfiguredLibrary(nameof(gameConfig.firstTurnDrawLibrary), gameConfig.firstTurnDrawLibrary, errors);
            ValidateConfiguredLibrary(nameof(gameConfig.normalTurnDrawLibrary), gameConfig.normalTurnDrawLibrary, errors);
            ValidateConfiguredLibrary(nameof(gameConfig.rewardLibrary), gameConfig.rewardLibrary, errors);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "[GameManager] GameConfig library validation failed:\n" + string.Join("\n", errors));
            }
        }

        private void ValidateLoadedCardEffects()
        {
            var errors = new List<string>();

            foreach (var card in CardDatabase.GetAll().Values)
            {
                if (card == null)
                {
                    continue;
                }

                ValidateEffectString(card, "preEffect", card.preEffect, errors);
                ValidateEffectString(card, "instantEffect", card.instantEffect, errors);
                ValidateEffectString(card, "settleEffect", card.settleEffect, errors);
                ValidateEffectString(card, "destroyEffect", card.destroyEffect, errors);
                ValidateTargetKind(card, errors);
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "[GameManager] Card validation failed:\n" + string.Join("\n", errors));
            }
        }

        private static void ValidateEffectString(CardData card, string fieldName, string effectString, List<string> errors)
        {
            if (CardEffectFactory.TryValidate(effectString, out var error))
            {
                return;
            }

            errors.Add($"Invalid {fieldName} on card [{card.cardId}] {card.cardName}: {effectString}. {error}");
        }

        private static void ValidateTargetKind(CardData card, List<string> errors)
        {
            if (CardTargeting.TryValidateConfiguredTargetKind(card, out var warning))
            {
                return;
            }

            errors.Add($"Card [{card.cardId}] {card.cardName} target kind mismatch. {warning}");
        }

        private static void ValidateConfiguredLibrary(string fieldName, CardLibrary library, List<string> errors)
        {
            if (library == null)
            {
                errors.Add($"GameConfig.{fieldName} is not assigned.");
                return;
            }

            try
            {
                CardLibraryDatabase.ValidateLibrary(library, $"GameConfig.{fieldName}");
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }
        }

        private void OnGameOver(GameEvents.GameOver e)
        {
            if (_turnFlowFsm != null && _turnFlowFsm.isRunning)
            {
                _turnFlowFsm.StopBehaviour(false);
            }
        }
    }
}

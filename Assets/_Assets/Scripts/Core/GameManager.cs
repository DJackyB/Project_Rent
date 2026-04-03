using BaoZuPo.Card;
using BaoZuPo.GameFlow;
using Martian.EventBus;
using NodeCanvas.StateMachines;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Core
{
    /// <summary>
    /// 游戏生命周期与系统初始化管理器。
    ///
    /// 责任：
    /// 1. Awake 阶段：初始化所有核心系统（卡牌库、经济系统、棋盘、卡组）
    /// 2. 配置加载与验证：从 GameConfig asset 读取各项参数，并校验卡牌库和卡牌效果
    /// 3. FSM 驱动：持有 turnFlowFsm 并在游戏结束时停止 FSM
    /// 4. 事件监听：订阅 GameOver 事件，清理游戏状态
    ///
    /// 初始化顺序（关键）：
    ///   1. CardEffectRegistration.EnsureRegistered() - 注册所有卡牌效果类
    ///   2. CardDatabase.LoadAll() - 加载所有卡牌资产
    ///   3. CardLibraryDatabase.LoadAll() - 加载卡牌库
    ///   4. 验证：库配置、库中卡牌、卡牌效果字符串
    ///   5. 获取场景中各大系统单例（MoneyManager、BoardManager、DeckManager）- 必须存在
    ///   6. 各系统初始化（传入起始参数）
    ///   7. 构建 GameContext（统一外部接口）
    ///
    /// 为什么这个顺序很重要：
    ///   - 卡牌效果必须先注册，否则后续验证会失败
    ///   - 库和卡牌必须加载，然后再去验证（快速失败）
    ///   - 各系统单例必须存在，Awake 阶段获取，否则后续逻辑崩溃
    ///   - GameContext 必须最后才能构建（所有系统都就位）
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Config")]
        public GameConfig gameConfig;

        /// <summary>
        /// FSM 驱动器，持有转法 FSM（准备→行动→结算→奖励→下一回合）
        /// </summary>
        private FSMOwner _turnFlowFsm;

        /// <summary>
        /// 游戏上下文：统一暴露 MoneyManager、BoardManager、DeckManager 等系统入口
        /// </summary>
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

        /// <summary>
        /// 初始化所有核心系统。
        /// 流程：卡牌库加载 → 验证 → 获取系统单例 → 各系统初始化 → 构建 GameContext
        /// 任何一步失败都会抛异常，中断游戏启动（快速失败原则）。
        /// </summary>
        private void InitializeSystems()
        {
            // Step 1: 注册所有卡牌效果类（必须最先做）
            CardEffectRegistration.EnsureRegistered();

            // Step 2: 加载卡牌数据库和卡牌库
            CardDatabase.LoadAll();
            CardLibraryDatabase.LoadAll();

            // Step 3: 验证配置和数据完整性（快速失败）
            ValidateConfiguredLibraries();
            ValidateLoadedCardEffects();

            // Step 4: 获取场景中各大系统单例（都是 Singleton）
            var moneyManager = Economy.MoneyManager.Instance;
            if (moneyManager == null)
            {
                throw new InvalidOperationException("[GameManager] MoneyManager is required in scene.");
            }

            var boardManager = Board.BoardManager.Instance;
            if (boardManager == null)
            {
                throw new InvalidOperationException("[GameManager] BoardManager is required in scene.");
            }

            var deckManager = Deck.DeckManager.Instance;
            if (deckManager == null)
            {
                throw new InvalidOperationException("[GameManager] DeckManager is required in scene.");
            }

            // Step 5: 各系统初始化（传入 GameConfig 参数）
            Debug.Log($"[GameManager] Config loaded. Money={gameConfig.startingMoney}, Rooms={gameConfig.initialRoomCount}, LoanGrowth={gameConfig.loanGrowthFactor}");
            moneyManager.Initialize(gameConfig.startingMoney);
            boardManager.Initialize(
                gameConfig.initialRoomCount,
                gameConfig.defaultTenantSlots,
                gameConfig.defaultEquipmentSlots
            );
            deckManager.Initialize(gameConfig.maxHandSize);

            // Step 6: 构建游戏上下文（统一接口）
            GameContext = new GameContext
            {
                MoneyManager = moneyManager,
                BoardManager = boardManager,
                DeckManager = deckManager,
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

        /// <summary>
        /// 游戏结束事件处理：停止 FSM
        /// </summary>
        private void OnGameOver(GameEvents.GameOver e)
        {
            if (_turnFlowFsm != null && _turnFlowFsm.isRunning)
            {
                _turnFlowFsm.StopBehaviour(false);
            }
        }
    }
}

using UnityEngine;
using BaoZuPo.Card;
using BaoZuPo.Card.Effects;

namespace BaoZuPo.Core
{
    /// <summary>
    /// Global Game Manager
    /// Entry point of the game, handles initialization of all sub-systems.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Configuration")]
        public GameConfig gameConfig;

        /// <summary>Game context passed to card effects</summary>
        public GameContext GameContext { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            if (gameConfig == null)
            {
                Debug.LogError("[GameManager] GameConfig is not assigned in the Inspector!");
                return;
            }

            InitializeSystems();
        }

        private void InitializeSystems()
        {
            // Register card effects
            RegisterCardEffects();

            // 1. Load card database
            CardDatabase.LoadAll();

            // 2. 初始化经济系统
            Debug.Log($"[GameManager] 读取配置: 初始资金={gameConfig.startingMoney}, 初始房间={gameConfig.initialRoomCount}");
            Economy.MoneyManager.Instance.Initialize(gameConfig.startingMoney);

            // 3. 初始化棋盘系统
            if (Board.BoardManager.Instance != null)
            {
                Board.BoardManager.Instance.Initialize(
                    gameConfig.initialRoomCount,
                    gameConfig.defaultTenantSlots,
                    gameConfig.defaultEquipmentSlots
                );
            }

            // 4. 初始化牌组系统
            if (Deck.DeckManager.Instance != null)
            {
                // Take values from the static database
                var allCards = CardDatabase.GetAll().Values;
                Deck.DeckManager.Instance.Initialize(allCards, gameConfig.maxHandSize);
            }

            // 5. Build game context
            GameContext = new GameContext
            {
                MoneyManager = Economy.MoneyManager.Instance,
                BoardManager = Board.BoardManager.Instance,
            };

            Debug.Log("[GameManager] 所有系统初始化完成");
        }

        private void RegisterCardEffects()
        {
            CardEffectFactory.Register("AddMoney", args => new AddMoneyEffect(int.Parse(args[0])));
            CardEffectFactory.Register("ReduceMoney", args => new ReduceMoneyEffect(int.Parse(args[0])));
            CardEffectFactory.Register("DrawCard", args => new DrawCardEffect(int.Parse(args[0])));
            CardEffectFactory.Register("ExpandSlot", args => new ExpandSlotEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddTenantDurability", args => new AddTenantDurabilityEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddEquipmentDurability", args => new AddEquipmentDurabilityEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddMoneyByEmptyRooms", args => new AddMoneyByEmptyRoomsEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddMoneyByRoomCount", args => new AddMoneyByRoomCountEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddTenantDurabilityInSelectedRoom", args => new AddTenantDurabilityInSelectedRoomEffect(int.Parse(args[0])));
            CardEffectFactory.Register("MoveTenantToEmptyRoom", _ => new MoveTenantToEmptyRoomEffect());
            CardEffectFactory.Register("EvictTenantInSelectedRoom", _ => new EvictTenantInSelectedRoomEffect());
            CardEffectFactory.Register("TriggerSelectedRoomSettle", _ => new TriggerSelectedRoomSettleEffect());
            CardEffectFactory.Register("SpawnRandomTenantInSelectedRoom", _ => new SpawnRandomTenantInSelectedRoomEffect());

            Debug.Log("[GameManager] 卡牌效果注册完成");
        }
    }
}

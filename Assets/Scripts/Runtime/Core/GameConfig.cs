using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.Core
{
    /// <summary>
    /// 游戏全局配置 ScriptableObject。
    /// 在 Editor 中创建一份 asset，通过 Inspector 手动调整参数。
    /// GameManager.Awake 时加载，任何变更都需要手工调整 Inspector 字段。
    ///
    /// 分组说明：
    /// - Economy：起始金额、贷款金额/间隔/增长因子
    /// - Draw：各阶段的抽卡数、手牌上限
    /// - Rooms：初始房间数、租户槽数、装备槽数
    /// - Feedback：浮动反馈系统的各项开关
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "BaoZuPo/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public const int MaxShopOfferCount = 3;

        [Header("Economy")]
        /// <summary>
        /// 游戏起始金额。
        /// </summary>
        [Tooltip("Starting money.")]
        public int startingMoney = 1000;

        /// <summary>
        /// 每个贷款周期扣除的金额（未经增长）。
        /// </summary>
        [Tooltip("Loan payment amount per cycle.")]
        public int loanAmount = 500;

        /// <summary>
        /// 贷款周期（转数）。例如 5 表示每 5 回合扣一次钱。
        /// </summary>
        [Tooltip("Number of turns between loan payments.")]
        public int loanInterval = 5;

        /// <summary>
        /// 贷款增长因子。每次贷款时的金额 = loanAmount * loanGrowthFactor^(支付次数)。
        /// 例如 factor=2 表示贷款额呈指数增长，压力逐个回合加重。
        /// </summary>
        [Tooltip("Growth factor applied to later loan payments.")]
        public float loanGrowthFactor = 2f;

        [Header("Draw")]
        /// <summary>
        /// 第一个回合抽取的卡牌数。
        /// </summary>
        [Tooltip("Cards drawn on the first turn.")]
        public int firstTurnDrawCount = 5;

        /// <summary>
        /// 普通回合抽取的卡牌数。
        /// </summary>
        [Tooltip("Cards drawn on a normal turn.")]
        public int normalTurnDrawCount = 3;

        /// <summary>
        /// 手牌上限（包括场景中获得的卡牌）。
        /// </summary>
        [Tooltip("Maximum hand size.")]
        public int maxHandSize = 7;

        /// <summary>
        /// 第一回合抽卡库。从此库中抽取 firstTurnDrawCount 张卡。
        /// </summary>
        [Tooltip("Card library used for the first turn draw.")]
        public CardLibrary firstTurnDrawLibrary;

        /// <summary>
        /// 普通回合抽卡库。从此库中抽取 normalTurnDrawCount 张卡。
        /// </summary>
        [Tooltip("Card library used for the normal turn draw.")]
        public CardLibrary normalTurnDrawLibrary;

        /// <summary>
        /// 奖励卡库。结算后三选一奖励从此库中选取。
        /// </summary>
        [Tooltip("Card library used for reward selection.")]
        public CardLibrary rewardLibrary;

        /// <summary>
        /// 商店候选卡牌库。打开商店时从这里抽取展示卡。
        /// </summary>
        [Tooltip("Card library used for shop offers.")]
        public CardLibrary shopLibrary;

        /// <summary>
        /// 每回合固定注入手牌的商店卡。
        /// </summary>
        [Tooltip("Card injected into hand every turn to open the shop.")]
        public CardData shopCard;

        /// <summary>
        /// 商店默认展示的唯一候选数量。
        /// </summary>
        [Tooltip("Number of unique cards shown in the shop.")]
        public int shopOfferCount = MaxShopOfferCount;

        [Header("Random Events")]
        /// <summary>
        /// 结算结束、三选一奖励出现前触发随机事件的概率。
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("Chance to trigger a random event after settlement before reward selection.")]
        public float postSettlementRandomEventChance = 0.25f;

        /// <summary>
        /// 结算后随机事件使用的事件库 ID。
        /// </summary>
        [Tooltip("Random event library id used after settlement.")]
        public string postSettlementRandomEventLibraryId = "lib_general";

        [Header("Rooms")]
        /// <summary>
        /// 游戏开始时已有的房间数。
        /// </summary>
        [Tooltip("Initial room count.")]
        public int initialRoomCount = 3;

        /// <summary>
        /// 每间房间默认的租户槽数（可通过卡牌扩展）。
        /// </summary>
        [Tooltip("Default tenant slot count per room.")]
        public int defaultTenantSlots = 1;

        /// <summary>
        /// 每间房间默认的装备槽数（可通过卡牌扩展）。
        /// </summary>
        [Tooltip("Default equipment slot count per room.")]
        public int defaultEquipmentSlots = 3;

        [Header("Feedback")]
        /// <summary>
        /// 是否启用浮动反馈模块（Martian.Feedback）。
        /// </summary>
        [Tooltip("Enable the shared floating feedback module.")]
        public bool enableFeedback = true;

        /// <summary>
        /// 是否显示金钱变化浮动反馈。
        /// </summary>
        [Tooltip("Enable money delta floating feedback.")]
        public bool enableMoneyFeedback = true;

        /// <summary>
        /// 是否在反馈模块中输出调试日志。
        /// </summary>
        [Tooltip("Enable debug logs for the feedback module.")]
        public bool enableFeedbackLogs = true;
    }
}

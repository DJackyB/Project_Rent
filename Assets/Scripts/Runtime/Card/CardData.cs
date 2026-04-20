using BaoZuPo.GameFlow;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌数据定义（ScriptableObject）。
    ///
    /// 包含卡牌的所有静态属性和配置，由 Excel 通过 CardDataImporter 导表生成。
    /// 每张卡牌对应一个 CardData.asset 资源文件，存储于 Resources/Cards/ 目录。
    ///
    /// 主要分类：
    /// - 基本信息：ID、名称、描述、类型、稀有度、美术资源
    /// - 游戏数值：花费、租金、耐久度、等待回合数
    /// - 目标配置：是否需要房间目标（装备/租客）或直接放置到场景
    /// - 效果配置：通过文本串定义四个生命周期效果（准备、即时、结算、摧毁）
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "BaoZuPo/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("唯一卡牌 ID。应与 Excel 来源中的卡牌 ID 保持一致。")]
        public int cardId;

        [Tooltip("卡牌显示名称。")]
        public string cardName;

        [Tooltip("卡牌描述文本。")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("卡牌类型：租客、装备、事件或合同。")]
        public CardType cardType;

        [Tooltip("卡牌稀有度（普通、稀有、史诗、传说）。")]
        public CardRarity rarity;

        [Tooltip("卡牌美术资源（头像）。")]
        public Sprite cardArt;

        [Tooltip("卡牌花费（金钱）。")]
        public int cost;

        [Tooltip("基础租金。仅租客卡牌使用。")]
        public int baseRent;

        [Header("Play Target")]
        [Tooltip("控制卡牌是否需要房间目标，或可以直接放置到场景区域。")]
        public CardPlayTargetKind targetKind = CardPlayTargetKind.PlayArea;

        [Header("Effect Configuration")]
        [Tooltip("准备阶段效果。格式示例：AddMoney;100")]
        public string preEffect;

        [Tooltip("即时效果，在卡牌被打出时立即结算。")]
        public string instantEffect;

        [Tooltip("结算效果，在结算阶段触发。")]
        public string settleEffect;

        [Tooltip("摧毁效果，当卡牌被摧毁时触发。")]
        public string destroyEffect;

        [Header("Tags")]
        [Tooltip("词条标签。由导表工具从 Excel 词条列写入，逗号分隔枚举名如 Quiet,Tidy。")]
        public TagType[] tags;

        [Header("Persistent Attributes")]
        [Tooltip("耐久度。通常在结算阶段减少。0 表示无限（不消耗）。")]
        public int durability;

        [Tooltip("等待回合数。每个回合结束时减少 1。0 表示无等待。")]
        public int waitTurns;
    }
}

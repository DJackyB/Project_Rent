using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌静态数据（ScriptableObject）
    /// 由 Excel 导表工具自动生成，也可在 Inspector 中手动编辑。
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "包租婆/CardData")]
    public class CardData : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("卡牌唯一ID，对应Excel中的卡牌ID")]
        public int cardId;

        [Tooltip("卡牌名称")]
        public string cardName;

        [Tooltip("卡牌说明文本")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("卡牌类型：租客/装备/事件")]
        public CardType cardType;

        [Tooltip("卡牌稀有度")]
        public CardRarity rarity;

        [Tooltip("卡面插图")]
        public Sprite cardArt;

        [Tooltip("使用花费（金钱）")]
        public int cost;

        [Header("效果配置")]
        [Tooltip("前置效果 - 每次准备阶段结算，格式如 AddMoney;100")]
        public string preEffect;

        [Tooltip("即时效果 - 出牌时结算")]
        public string instantEffect;

        [Tooltip("结算效果 - 回合结束时结算")]
        public string settleEffect;

        [Tooltip("销毁效果 - 卡牌因耐久归零销毁时结算")]
        public string destroyEffect;

        [Header("持续性")]
        [Tooltip("耐久：每次准备阶段 -1，归零时销毁。0 表示无限耐久")]
        public int durability;

        [Tooltip("等待回合数：每回合末 -1，归零时销毁（不触发销毁效果）。0 表示无等待")]
        public int waitTurns;
    }
}

using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌静态数据 ScriptableObject
    /// 可由 Excel 导入工具生成 也支持在 Inspector 中手动维护
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "BaoZuPo/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("\u57fa\u7840\u4fe1\u606f")]
        [Tooltip("\u5361\u724c\u552f\u4e00 ID \u9700\u8981\u4e0e Excel \u914d\u8868\u4e2d\u7684\u5361\u724c ID \u5bf9\u5e94")]
        public int cardId;

        [Tooltip("\u5361\u724c\u540d\u79f0")]
        public string cardName;

        [Tooltip("\u5361\u724c\u63cf\u8ff0\u6587\u672c")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("\u5361\u724c\u7c7b\u578b \u79df\u5ba2 \u88c5\u5907 \u4e8b\u4ef6 \u5408\u540c")]
        public CardType cardType;

        [Tooltip("\u5361\u724c\u7a00\u6709\u5ea6")]
        public CardRarity rarity;

        [Tooltip("\u5361\u724c\u63d2\u56fe")]
        public Sprite cardArt;

        [Tooltip("\u6253\u51fa\u8d39\u7528")]
        public int cost;

        [Tooltip("\u57fa\u7840\u79df\u91d1 \u4ec5\u79df\u5ba2\u5361\u4f7f\u7528")]
        public int baseRent;

        [Header("\u6548\u679c\u914d\u7f6e")]
        [Tooltip("\u51c6\u5907\u9636\u6bb5\u6548\u679c \u793a\u4f8b AddMoney;100")]
        public string preEffect;

        [Tooltip("\u5373\u65f6\u6548\u679c \u5728\u6253\u51fa\u5361\u724c\u65f6\u7ed3\u7b97")]
        public string instantEffect;

        [Tooltip("\u7ed3\u7b97\u6548\u679c \u5728\u56de\u5408\u7ed3\u7b97\u9636\u6bb5\u89e6\u53d1")]
        public string settleEffect;

        [Tooltip("\u9500\u6bc1\u6548\u679c \u5728\u5361\u724c\u88ab\u9500\u6bc1\u65f6\u89e6\u53d1")]
        public string destroyEffect;

        [Header("\u6301\u7eed\u5c5e\u6027")]
        [Tooltip("\u8010\u4e45 \u901a\u5e38\u5728\u7ed3\u7b97\u9636\u6bb5\u9012\u51cf 0 \u8868\u793a\u65e0\u9650")]
        public int durability;

        [Tooltip("\u7b49\u5f85\u56de\u5408\u6570 \u6bcf\u56de\u5408\u7ed3\u675f\u65f6\u9012\u51cf 1 0 \u8868\u793a\u65e0\u7b49\u5f85")]
        public int waitTurns;
    }
}

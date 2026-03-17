namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌稀有度
    /// </summary>
    public enum CardRarity
    {
        /// <summary>普通 - 灰色</summary>
        Common = 0,

        /// <summary>稀有 - 蓝色</summary>
        Rare = 1,

        /// <summary>史诗 - 紫色（Excel配表中的"史诗"）</summary>
        Epic = 2,

        /// <summary>传说 - 黄色</summary>
        Legendary = 3
    }
}

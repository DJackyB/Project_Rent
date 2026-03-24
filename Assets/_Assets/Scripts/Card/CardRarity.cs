namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌稀有度。
    /// </summary>
    public enum CardRarity
    {
        /// <summary>普通，常用灰色表现</summary>
        Common = 0,

        /// <summary>稀有，常用蓝色表现</summary>
        Rare = 1,

        /// <summary>史诗，常用紫色表现，对应 Excel 中的“史诗”</summary>
        Epic = 2,

        /// <summary>传说，常用金黄色表现</summary>
        Legendary = 3
    }
}

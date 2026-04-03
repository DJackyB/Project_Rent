namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌稀有度枚举。
    ///
    /// 从普通到传说，代表卡牌的获得难度和威力。
    /// 用于 UI 显示（边框颜色、图标）和平衡调整。
    /// 值对应 Excel 中的稀有度编号。
    /// </summary>
    public enum CardRarity
    {
        /// <summary>普通稀有度。常用灰色表现。</summary>
        Common = 0,

        /// <summary>稀有稀有度。常用蓝色表现。</summary>
        Rare = 1,

        /// <summary>史诗稀有度。常用紫色表现。对应 Excel 中的”史诗”。</summary>
        Epic = 2,

        /// <summary>传说稀有度。常用金黄色表现。</summary>
        Legendary = 3
    }
}

namespace BaoZuPo.Integration.Martian.Tooltip
{
    /// <summary>
    /// 提示内容 ID 映射表。
    /// 定义游戏中各类提示的唯一标识符，供 TooltipTrigger 和 TooltipPresenter 使用。
    /// 当用户悬停卡牌时，TooltipTrigger 发送此 ID，提示系统根据 ID 匹配对应的呈现器。
    /// </summary>
    public static class BaoZuPoTooltipContentIds
    {
        /// <summary>
        /// 卡牌预览提示内容 ID。
        /// TooltipTrigger 标记卡牌悬停时发送此 ID，BaoZuPoCardTooltipPresenter 据此生成卡牌预览。
        /// </summary>
        public const string CardPreview = "baozupo.card.preview";
    }
}

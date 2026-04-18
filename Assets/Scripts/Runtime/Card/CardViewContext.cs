namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌在不同 UI 场景下的显示上下文。
    ///
    /// 用于控制同一张卡牌对象在不同位置、不同交互模式下的显隐规则。
    /// UI 系统可根据当前上下文决定显示哪些信息、启用哪些交互。
    /// </summary>
    public enum CardViewContext
    {
        /// <summary>手牌中。卡牌在玩家手中，可被打出。</summary>
        Hand,

        /// <summary>房间中作为租客。卡牌已放置到房间，显示租金和耐久度。</summary>
        RoomTenant,

        /// <summary>房间中作为装备。卡牌已放置到房间辅助租客，显示增益效果。</summary>
        RoomEquipment,

        /// <summary>合同卡位置。卡牌作为合同常驻，显示长期效果。</summary>
        Contract,

        /// <summary>悬停预览面板。临时展示卡牌详细信息（描述、效果解析）。</summary>
        TooltipPreview,

        /// <summary>奖励选择。卡牌在奖励阶段的选择面板中。</summary>
        RewardPick
    }
}

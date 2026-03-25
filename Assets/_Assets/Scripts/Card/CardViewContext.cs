namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌在不同 UI 场景下的显示上下文。
    /// 用于控制同一张卡在手牌、场上、悬停预览中的显隐规则。
    /// </summary>
    public enum CardViewContext
    {
        Hand,
        RoomTenant,
        RoomEquipment,
        Contract,
        TooltipPreview
    }
}

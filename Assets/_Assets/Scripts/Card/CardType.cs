namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌类型
    /// </summary>
    public enum CardType
    {
        /// <summary>租客卡 - 可放置于房间，默认存在结算效果</summary>
        Tenant,

        /// <summary>装备卡 - 可放置于房间</summary>
        Equipment,

        /// <summary>事件卡 - 普通/随机/全局</summary>
        Event,

        /// <summary>合同卡 - 常驻结算，类似遗物</summary>
        Contract
    }
}

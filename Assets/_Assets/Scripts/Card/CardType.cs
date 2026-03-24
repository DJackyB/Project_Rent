namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌类型。
    /// </summary>
    public enum CardType
    {
        /// <summary>租客卡，可放置到房间中，通常带有结算收益</summary>
        Tenant,

        /// <summary>装备卡，可放置到房间中辅助租客</summary>
        Equipment,

        /// <summary>事件卡，通常为即时结算或随机/全局效果</summary>
        Event,

        /// <summary>合同卡，常驻生效，类似遗物或长期规则修正</summary>
        Contract
    }
}

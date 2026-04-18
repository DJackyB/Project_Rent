namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌类型枚举。
    ///
    /// 定义卡牌在游戏循环中的角色和行为特征。
    /// 影响卡牌的效果生命周期、目标配置和 UI 显示方式。
    /// </summary>
    public enum CardType
    {
        /// <summary>租客卡。可放置到房间中，通常带有结算收益（基础租金）。</summary>
        Tenant,

        /// <summary>装备卡。可放置到房间中辅助租客（增加租金、耐久度等）。</summary>
        Equipment,

        /// <summary>事件卡。通常为即时效果或全局效果，不需要房间目标。</summary>
        Event,

        /// <summary>合同卡。常驻生效，类似遗物或长期规则修正，多用于准备阶段效果。</summary>
        Contract
    }
}

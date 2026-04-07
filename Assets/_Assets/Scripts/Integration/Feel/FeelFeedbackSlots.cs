namespace BaoZuPo.Integration.Feel
{
    /// <summary>
    /// Feel 反馈 slot 名称常量。
    /// 每个 slot 对应一个预配置的 MMF_Player 预制体，由 Installer 注册。
    /// UI 层直接调用时也使用这些常量。
    /// </summary>
    public static class FeelFeedbackSlots
    {
        /// <summary>金币增减（出牌费用、即时收益、结算总金额）。</summary>
        public const string MoneyDelta = "MoneyDelta";

        /// <summary>结算步骤（房间/合约锚点 pulse / 高亮）。</summary>
        public const string SettlementStep = "SettlementStep";

        /// <summary>贷款还款（红色 UI 闪烁 / 金额强调）。</summary>
        public const string LoanPayment = "LoanPayment";

        /// <summary>出牌成功（轻微缩放 / 光效）。</summary>
        public const string CardPlay = "CardPlay";

        /// <summary>奖励三选一（淡入 / 选中反馈）。</summary>
        public const string RewardReveal = "RewardReveal";
    }
}

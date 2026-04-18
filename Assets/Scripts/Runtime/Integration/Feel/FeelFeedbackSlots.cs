namespace BaoZuPo.Integration.Feel
{
    /// <summary>
    /// Stable Feel slot names used by UI and Martian.Feedback integration points.
    /// Slots are currently unregistered; add MMF_Player registrations only after a single effect is rebuilt and verified.
    /// </summary>
    public static class FeelFeedbackSlots
    {
        public const string MoneyDelta = "MoneyDelta";
        public const string SettlementStep = "SettlementStep";
        public const string LoanPayment = "LoanPayment";
        public const string CardPlay = "CardPlay";
        public const string RewardReveal = "RewardReveal";
        public const string MoneyPulse = "MoneyPulse";
        public const string CardHover = "CardHover";
        public const string CardDropAccepted = "CardDropAccepted";
        public const string CardDropRejected = "CardDropRejected";
        public const string PhaseChange = "PhaseChange";
        public const string GameOver = "GameOver";
        public const string RewardSelected = "RewardSelected";
        public const string HandDraw = "HandDraw";
        public const string LoanWarning = "LoanWarning";
        public const string RoomExpanded = "RoomExpanded";
    }
}

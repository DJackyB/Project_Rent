namespace BaoZuPo.Integration.Martian.Feedback
{
    /// <summary>
    /// 包租婆项目的反馈类别常量。
    /// 对应 FeedbackRequest.Category / FeedbackStep.Category 字段。
    /// </summary>
    public static class BaoZuPoFeedbackCategories
    {
        public const string Money = "money";
        public const string Settlement = "settlement";
        public const string Cost = "cost";
        public const string Loan = "loan";
    }

    /// <summary>
    /// 包租婆项目的反馈目标类型常量。
    /// 对应 FeedbackRequest.TargetKind / FeedbackSequenceRequest.TargetKind 字段。
    /// </summary>
    public static class BaoZuPoFeedbackTargetKinds
    {
        public const string Room = "room";
        public const string Card = "card";
        public const string Global = "global";
    }
}

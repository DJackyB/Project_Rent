namespace Martian.Feedback
{
    /// <summary>
    /// 通用反馈通道。
    /// </summary>
    public enum FeedbackChannel
    {
        FloatingText,
        Sequence
    }

    /// <summary>
    /// 框架内置的默认类别标签。项目可定义自己的字符串常量。
    /// </summary>
    public static class FeedbackCategories
    {
        public const string Default = "default";
    }

    /// <summary>
    /// 框架内置的默认目标类型标签。项目可定义自己的字符串常量。
    /// </summary>
    public static class FeedbackTargetKinds
    {
        public const string Global = "global";
    }
}

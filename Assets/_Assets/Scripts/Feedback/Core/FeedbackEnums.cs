using UnityEngine;

namespace BaoZuPo.Feedback.Core
{
    public enum FeedbackCategory
    {
        Money,
        Settlement,
        Cost,
        Loan
    }

    public enum FeedbackTargetKind
    {
        Room,
        Card,
        Global
    }

    public enum FeedbackChannel
    {
        FloatingText,
        Sequence
    }
}

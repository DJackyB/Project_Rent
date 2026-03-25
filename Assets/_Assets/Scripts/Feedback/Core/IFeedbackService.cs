namespace BaoZuPo.Feedback.Core
{
    public interface IFeedbackService
    {
        bool IsAvailable { get; }

        void Publish(FeedbackRequest request);
        void PublishSequence(FeedbackSequenceRequest request);
    }
}

namespace Martian.Feedback
{
    public interface IFeedbackService
    {
        bool IsAvailable { get; }

        void Publish(FeedbackRequest request);
        void PublishSequence(FeedbackSequenceRequest request);
    }
}

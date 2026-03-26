namespace Martian.Feedback
{
    public interface IFeedbackService
    {
        bool IsAvailable { get; }

        FeedbackPlaybackHandle Publish(FeedbackRequest request);
        FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request);
    }
}

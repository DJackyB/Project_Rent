using Martian.Feedback;

namespace Martian.Feedback.Runtime
{
    public sealed class NoOpFeedbackService : IFeedbackService
    {
        public static readonly NoOpFeedbackService Instance = new();

        public bool IsAvailable => false;

        private NoOpFeedbackService()
        {
        }

        public void Publish(FeedbackRequest request)
        {
        }

        public void PublishSequence(FeedbackSequenceRequest request)
        {
        }
    }
}

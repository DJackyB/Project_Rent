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

        public FeedbackPlaybackHandle Publish(FeedbackRequest request)
        {
            var handle = new FeedbackPlaybackHandle(request != null ? request.LaneKey : null, request != null ? request.TargetKey : null);
            handle.Cancel();
            return handle;
        }

        public FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request)
        {
            var handle = new FeedbackPlaybackHandle(request != null ? request.LaneKey : null, request != null ? request.TargetKey : null);
            handle.Cancel();
            return handle;
        }
    }
}

using UnityEngine;

namespace Martian.Feedback.Runtime
{
    public interface IFeedbackPlaybackBackend
    {
        bool IsAvailable { get; }

        event System.Action AllPlaybackCompleted;

        void Attach(Transform host);
        void Configure(FeedbackRuntimeOptions options);
        FeedbackPlaybackHandle Publish(FeedbackRequest request);
        FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request);
        void Clear();
    }
}

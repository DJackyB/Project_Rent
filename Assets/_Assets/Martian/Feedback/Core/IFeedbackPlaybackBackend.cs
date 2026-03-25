using UnityEngine;

namespace Martian.Feedback.Runtime
{
    public interface IFeedbackPlaybackBackend
    {
        bool IsAvailable { get; }

        event System.Action AllPlaybackCompleted;

        void Attach(Transform host);
        void Configure(FeedbackRuntimeOptions options);
        void Publish(FeedbackRequest request);
        void PublishSequence(FeedbackSequenceRequest request);
        void Clear();
    }
}

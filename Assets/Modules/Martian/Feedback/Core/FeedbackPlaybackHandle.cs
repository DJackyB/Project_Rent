using System;

namespace Martian.Feedback
{
    public sealed class FeedbackPlaybackHandle
    {
        public string RequestId { get; }
        public string LaneKey { get; }
        public string TargetKey { get; }

        public bool IsCompleted { get; private set; }
        public bool IsCancelled { get; private set; }
        public bool IsFinished => IsCompleted || IsCancelled;

        public event Action<FeedbackPlaybackHandle> Completed;
        public event Action<FeedbackPlaybackHandle> Cancelled;

        internal FeedbackPlaybackHandle(string laneKey, string targetKey)
        {
            RequestId = Guid.NewGuid().ToString("N");
            LaneKey = string.IsNullOrWhiteSpace(laneKey) ? "__global__" : laneKey;
            TargetKey = targetKey ?? string.Empty;
        }

        internal void Complete()
        {
            if (IsFinished)
            {
                return;
            }

            IsCompleted = true;
            Completed?.Invoke(this);
        }

        internal void Cancel()
        {
            if (IsFinished)
            {
                return;
            }

            IsCancelled = true;
            Cancelled?.Invoke(this);
        }
    }
}

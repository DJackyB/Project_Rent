using System;
using Martian.Feedback;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Martian.Feedback.Runtime
{
    [MovedFrom("BaoZuPo.Feedback.Runtime")]
    public class FeedbackPlaybackCoordinator : MonoBehaviour, IFeedbackService
    {
        private IFeedbackPlaybackBackend _backend = new FloatingTextFeedbackBackend();
        private FeedbackRuntimeOptions _options = new();

        public event Action AllPlaybackCompleted;

        public bool IsAvailable => isActiveAndEnabled && _backend != null && _backend.IsAvailable;

        public void Configure(FeedbackRuntimeOptions options)
        {
            _options = options != null ? options.Clone() : new FeedbackRuntimeOptions();
            BindBackend();
        }

        public void SetBackend(IFeedbackPlaybackBackend backend)
        {
            UnbindBackend();
            _backend?.Clear();
            _backend = backend ?? new FloatingTextFeedbackBackend();
            BindBackend();
        }

        public FeedbackPlaybackHandle Publish(FeedbackRequest request)
        {
            if (!IsAvailable)
            {
                return CreateCancelledHandle(request != null ? request.LaneKey : null, request != null ? request.TargetKey : null);
            }

            return _backend.Publish(request);
        }

        public FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request)
        {
            if (!IsAvailable)
            {
                return CreateCancelledHandle(request != null ? request.LaneKey : null, request != null ? request.TargetKey : null);
            }

            return _backend.PublishSequence(request);
        }

        public void Clear()
        {
            _backend?.Clear();
        }

        private void OnEnable()
        {
            BindBackend();
        }

        private void OnDisable()
        {
            UnbindBackend();
            _backend?.Clear();
        }

        private void BindBackend()
        {
            if (_backend == null)
            {
                return;
            }

            _backend.Attach(transform);
            _backend.Configure(_options);
            _backend.AllPlaybackCompleted -= HandleBackendCompletion;
            _backend.AllPlaybackCompleted += HandleBackendCompletion;
        }

        private void UnbindBackend()
        {
            if (_backend == null)
            {
                return;
            }

            _backend.AllPlaybackCompleted -= HandleBackendCompletion;
        }

        private void HandleBackendCompletion()
        {
            AllPlaybackCompleted?.Invoke();
        }

        private static FeedbackPlaybackHandle CreateCancelledHandle(string laneKey, string targetKey)
        {
            var handle = new FeedbackPlaybackHandle(laneKey, targetKey);
            handle.Cancel();
            return handle;
        }
    }
}

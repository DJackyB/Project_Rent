using System;
using Martian.Feedback;
using Martian.Feedback.Runtime;
using UnityEngine;

namespace BaoZuPo.Integration.Feel
{
    /// <summary>
    /// 组合反馈后端。将主后端（FloatingText）和副后端（Feel）组合在一起。
    /// 主后端控制时序并返回 handle，副后端只做视觉增强，其返回值被丢弃。
    /// 副后端异常不会阻断主后端。
    /// </summary>
    public sealed class CompositeFeedbackPlaybackBackend : IFeedbackPlaybackBackend
    {
        private readonly IFeedbackPlaybackBackend _primary;
        private readonly IFeedbackPlaybackBackend _secondary;

        public CompositeFeedbackPlaybackBackend(IFeedbackPlaybackBackend primary, IFeedbackPlaybackBackend secondary)
        {
            _primary = primary;
            _secondary = secondary;
        }

        /// <summary>取主后端的可用性。副后端不影响此判断。</summary>
        public bool IsAvailable => _primary != null && _primary.IsAvailable;

        /// <summary>只订阅主后端的完成事件。副后端不参与时序。</summary>
        public event Action AllPlaybackCompleted
        {
            add
            {
                if (_primary != null) _primary.AllPlaybackCompleted += value;
            }
            remove
            {
                if (_primary != null) _primary.AllPlaybackCompleted -= value;
            }
        }

        public void Attach(Transform host)
        {
            _primary?.Attach(host);
            TrySecondary(() => _secondary?.Attach(host));
        }

        public void Configure(FeedbackRuntimeOptions options)
        {
            _primary?.Configure(options);
            TrySecondary(() => _secondary?.Configure(options));
        }

        public FeedbackPlaybackHandle Publish(FeedbackRequest request)
        {
            TrySecondary(() => _secondary?.Publish(request));
            return _primary != null ? _primary.Publish(request) : null;
        }

        public FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request)
        {
            TrySecondary(() => _secondary?.PublishSequence(request));
            return _primary != null ? _primary.PublishSequence(request) : null;
        }

        public void Clear()
        {
            TrySecondary(() => _secondary?.Clear());
            _primary?.Clear();
        }

        private static void TrySecondary(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[CompositeFeedback] 副后端异常（不影响主后端）：{e.Message}");
            }
        }
    }
}

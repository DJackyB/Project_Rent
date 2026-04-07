using System;
using Martian.Feedback;
using TMPro;
using UnityEngine;

namespace Martian.Feedback.Runtime
{
    internal interface IFeedbackFontResolver
    {
        void SetFontResolver(Func<TMP_FontAsset> fontResolver);
    }

    /// <summary>
    /// 反馈播放协调器。实现 IFeedbackService 接口，作为反馈发布的主要入口。
    ///
    /// 职责：
    /// - 管理反馈后端（如浮动文本后端）的生命周期
    /// - 转发反馈请求到后端的 Publish / PublishSequence
    /// - 监听后端的 AllPlaybackCompleted 事件，并转发给上游
    /// - 提供 Configure() 和 SetBackend() 用于运行时改变配置
    ///
    /// 使用流程：
    /// 1. FeedbackBootstrap 创建此协调器
    /// 2. 协调器被注册到 FeedbackServiceLocator
    /// 3. 其他代码通过 IFeedbackService 发布反馈
    /// 4. 协调器将请求转发给当前后端执行
    /// </summary>
    public class FeedbackPlaybackCoordinator : MonoBehaviour, IFeedbackService
    {
        /// <summary>当前活跃的反馈后端实现（默认为浮动文本）。</summary>
        private IFeedbackPlaybackBackend _backend = new FloatingTextFeedbackBackend();

        /// <summary>当前运行时配置选项。</summary>
        private FeedbackRuntimeOptions _options = new();
        private Func<TMP_FontAsset> _fontResolver;

        /// <summary>所有活跃反馈播放完成时触发。监听后端事件并转发。</summary>
        public event Action AllPlaybackCompleted;

        /// <summary>协调器是否可用（本身启用且后端可用）。</summary>
        public bool IsAvailable => isActiveAndEnabled && _backend != null && _backend.IsAvailable;

        /// <summary>
        /// 配置运行时选项。
        /// 选项会传递给后端，影响反馈播放行为（如排序顺序、启用状态等）。
        /// </summary>
        /// <param name="options">运行时选项</param>
        public void Configure(FeedbackRuntimeOptions options)
        {
            _options = options != null ? options.Clone() : new FeedbackRuntimeOptions();
            BindBackend();
        }

        /// <summary>
        /// 切换反馈后端实现。
        /// 流程：
        /// 1. 解绑当前后端的事件
        /// 2. 清理当前后端资源
        /// 3. 使用新后端（如果为 null，使用默认浮动文本后端）
        /// 4. 重新绑定新后端
        /// </summary>
        /// <param name="backend">新的后端实现，或 null 使用默认</param>
        public void SetBackend(IFeedbackPlaybackBackend backend)
        {
            UnbindBackend();
            _backend?.Clear();
            _backend = backend ?? new FloatingTextFeedbackBackend();
            BindBackend();
        }

        public void SetFontResolver(Func<TMP_FontAsset> fontResolver)
        {
            _fontResolver = fontResolver;
            ApplyFontResolver();
        }

        /// <summary>
        /// 发布单条反馈请求。
        /// 如果协调器不可用，返回一个已取消的句柄。
        /// </summary>
        /// <param name="request">反馈请求</param>
        /// <returns>反馈播放句柄</returns>
        public FeedbackPlaybackHandle Publish(FeedbackRequest request)
        {
            if (!IsAvailable)
            {
                return CreateCancelledHandle(request != null ? request.LaneKey : null, request != null ? request.TargetKey : null);
            }

            return _backend.Publish(request);
        }

        /// <summary>
        /// 发布反馈序列（多步反馈）。
        /// 如果协调器不可用，返回一个已取消的句柄。
        /// </summary>
        /// <param name="request">反馈序列请求</param>
        /// <returns>反馈播放句柄</returns>
        public FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request)
        {
            if (!IsAvailable)
            {
                return CreateCancelledHandle(request != null ? request.LaneKey : null, request != null ? request.TargetKey : null);
            }

            return _backend.PublishSequence(request);
        }

        /// <summary>
        /// 清理所有运行中的反馈播放。通常在卸载或禁用时调用。
        /// </summary>
        public void Clear()
        {
            _backend?.Clear();
        }

        /// <summary>启用时绑定后端。</summary>
        private void OnEnable()
        {
            BindBackend();
        }

        /// <summary>禁用时解绑并清理后端。</summary>
        private void OnDisable()
        {
            UnbindBackend();
            _backend?.Clear();
        }

        /// <summary>
        /// 将后端附加到此协调器的 Transform，并订阅其完成事件。
        /// </summary>
        private void BindBackend()
        {
            if (_backend == null)
            {
                return;
            }

            _backend.Attach(transform);
            _backend.Configure(_options);
            ApplyFontResolver();
            _backend.AllPlaybackCompleted -= HandleBackendCompletion;
            _backend.AllPlaybackCompleted += HandleBackendCompletion;
        }

        /// <summary>
        /// 从后端解除事件订阅。
        /// </summary>
        private void UnbindBackend()
        {
            if (_backend == null)
            {
                return;
            }

            _backend.AllPlaybackCompleted -= HandleBackendCompletion;
        }

        /// <summary>
        /// 处理后端的播放完成事件，转发给上游监听者。
        /// </summary>
        private void HandleBackendCompletion()
        {
            AllPlaybackCompleted?.Invoke();
        }

        private void ApplyFontResolver()
        {
            if (_backend is IFeedbackFontResolver fontAwareBackend)
            {
                fontAwareBackend.SetFontResolver(_fontResolver);
            }
        }

        /// <summary>
        /// 创建一个已取消的反馈句柄。用于反馈无法播放时返回。
        /// </summary>
        private static FeedbackPlaybackHandle CreateCancelledHandle(string laneKey, string targetKey)
        {
            var handle = new FeedbackPlaybackHandle(laneKey, targetKey);
            handle.Cancel();
            return handle;
        }
    }
}

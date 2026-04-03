using UnityEngine;

namespace Martian.Feedback.Runtime
{
    /// <summary>
    /// 反馈播放后端接口。定义反馈实现的核心职责。
    /// 各个后端（如浮动文本、音效等）都应实现此接口。
    /// </summary>
    public interface IFeedbackPlaybackBackend
    {
        /// <summary>
        /// 后端是否可用（例如已挂载到 Canvas）。
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 所有播放任务完成时触发。用于知道何时反馈系统空闲。
        /// </summary>
        event System.Action AllPlaybackCompleted;

        /// <summary>
        /// 将后端附加到指定的 Transform（通常是 Canvas 或 UI 根节点）。
        /// </summary>
        /// <param name="host">宿主 Transform，用于放置后端的 UI 或音源对象</param>
        void Attach(Transform host);

        /// <summary>
        /// 配置运行时选项（排序顺序、启用状态等）。
        /// </summary>
        /// <param name="options">运行时选项对象</param>
        void Configure(FeedbackRuntimeOptions options);

        /// <summary>
        /// 发布单条反馈。
        /// </summary>
        /// <param name="request">反馈请求</param>
        /// <returns>反馈播放句柄，用于追踪状态或取消播放</returns>
        FeedbackPlaybackHandle Publish(FeedbackRequest request);

        /// <summary>
        /// 发布反馈序列（多步反馈）。
        /// </summary>
        /// <param name="request">反馈序列请求</param>
        /// <returns>反馈播放句柄</returns>
        FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request);

        /// <summary>
        /// 清理所有运行中的播放和资源。通常在卸载或禁用时调用。
        /// </summary>
        void Clear();
    }
}

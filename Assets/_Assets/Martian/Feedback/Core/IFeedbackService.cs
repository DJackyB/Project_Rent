namespace Martian.Feedback
{
    /// <summary>
    /// 反馈服务接口。负责发布玩家可见的反馈消息（如浮动文本、音效等）。
    /// 由具体的后端实现（如 FloatingTextFeedbackBackend）承载。
    /// </summary>
    public interface IFeedbackService
    {
        /// <summary>
        /// 服务是否可用。用于检查后端是否已初始化。
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 发布单条反馈消息。
        /// </summary>
        /// <param name="request">反馈请求，包含文本、目标、位置等信息</param>
        /// <returns>反馈播放句柄，用于追踪播放状态</returns>
        FeedbackPlaybackHandle Publish(FeedbackRequest request);

        /// <summary>
        /// 发布一个反馈序列（多步反馈，按顺序播放）。
        /// </summary>
        /// <param name="request">反馈序列请求</param>
        /// <returns>反馈播放句柄</returns>
        FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request);
    }
}

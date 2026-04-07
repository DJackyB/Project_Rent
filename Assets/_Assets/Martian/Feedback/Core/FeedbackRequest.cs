using System;
using UnityEngine;

namespace Martian.Feedback
{
    /// <summary>
    /// 单条反馈请求数据结构。用于发布可见反馈（如浮动文本）。
    ///
    /// 关键字段：
    /// - Text / NumericDelta：显示内容
    /// - Anchor：反馈放置的锚点（UI 元素）
    /// - ScreenOffset：相对屏幕的偏移
    /// - LaneKey / TargetKey：用于分组多个反馈（相同 Lane 的反馈顺序播放）
    /// - Category：反馈类型（金钱、伤害等），影响颜色/样式
    /// - Priority：优先级（高优先级的反馈可能插队）
    /// </summary>
    [Serializable]
    public class FeedbackRequest
    {
        /// <summary>调试用标签，不影响播放。</summary>
        public string DebugLabel;

        /// <summary>
        /// 车道键。相同 LaneKey 的反馈会在同一个播放队列中，按顺序执行。
        /// 若为空，使用 TargetKey 作为 LaneKey；都空则使用全局车道。
        /// </summary>
        public string LaneKey;

        /// <summary>
        /// 目标键。标识反馈的播放目标（如角色ID）。
        /// 用于锚点位置或事件追踪。
        /// </summary>
        public string TargetKey;

        /// <summary>目标类型。决定反馈如何锚定到世界或屏幕。</summary>
        public string TargetKind = FeedbackTargetKinds.Global;

        /// <summary>反馈渠道。决定使用哪种后端（如浮动文本、音效等）。</summary>
        public FeedbackChannel Channel = FeedbackChannel.FloatingText;

        /// <summary>
        /// UI 锚点。反馈将相对此 RectTransform 显示。
        /// 若为 null，使用屏幕中心或 ScreenOffset。
        /// </summary>
        public RectTransform Anchor;

        /// <summary>
        /// 当 Anchor 不存在或非活跃时，是否回落到屏幕中心。
        /// </summary>
        public bool UseScreenCenterFallback = true;

        /// <summary>相对屏幕的像素偏移。默认向上 96 像素。</summary>
        public Vector2 ScreenOffset = new Vector2(0f, 96f);

        /// <summary>反馈显示的文本内容（如 "+100 金币"）。</summary>
        public string Text;

        /// <summary>数值变化量（如实际增加/减少的金币数）。可用于分析或特殊效果。</summary>
        public int NumericDelta;

        /// <summary>反馈类别。影响样式、颜色、音效等。</summary>
        public string Category = FeedbackCategories.Default;

        /// <summary>优先级。数值越高越优先。高优先级反馈可能会先执行。</summary>
        public int Priority;
    }
}

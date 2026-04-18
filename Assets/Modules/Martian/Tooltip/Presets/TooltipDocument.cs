using System;
using System.Collections.Generic;
using System.Linq;

namespace Martian.Tooltip.Presets
{
    /// <summary>
    /// 文档化提示内容。用于显示结构化的提示信息（如卡牌说明）。
    ///
    /// 结构：
    /// - Title：主标题（如卡牌名称）
    /// - Subtitle：副标题（如卡牌类型）
    /// - Summary：摘要（一句话描述）
    /// - Tags：标签列表（如 "技能", "法术"）
    /// - Sections：详细章节列表（如规则、效果等）
    ///
    /// 用途：
    /// - 创建结构化的提示框（区别于简单的浮动文本）
    /// - 配合 TooltipDocumentPresenter 显示成多行格式
    /// - 可被序列化，存储在 ScriptableObject 中
    ///
    /// 示例：
    /// new TooltipDocument(
    ///     title: "火球术",
    ///     subtitle: "法术",
    ///     summary: "造成 10 点伤害",
    ///     tags: new[] { "伤害", "法术" },
    ///     sections: new[] {
    ///         new TooltipDocumentSection("效果", "对目标造成 10 点火焰伤害")
    ///     }
    /// )
    /// </summary>
    [Serializable]
    public sealed class TooltipDocument
    {
        /// <summary>主标题。必需。</summary>
        public string Title { get; }

        /// <summary>副标题。可选。</summary>
        public string Subtitle { get; }

        /// <summary>摘要。一句话描述。可选。</summary>
        public string Summary { get; }

        /// <summary>标签列表（已过滤空值）。</summary>
        public IReadOnlyList<string> Tags { get; }

        /// <summary>详细章节列表（已过滤空值）。</summary>
        public IReadOnlyList<TooltipDocumentSection> Sections { get; }

        /// <summary>
        /// 构造文档化提示。
        /// 自动过滤空的标签和章节。
        /// </summary>
        public TooltipDocument(
            string title,
            string subtitle = null,
            string summary = null,
            IEnumerable<string> tags = null,
            IEnumerable<TooltipDocumentSection> sections = null)
        {
            Title = title;
            Subtitle = subtitle;
            Summary = summary;
            // 过滤空标签
            Tags = tags != null
                ? tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray()
                : Array.Empty<string>();
            // 过滤空章节
            Sections = sections != null
                ? sections.Where(section => section != null).ToArray()
                : Array.Empty<TooltipDocumentSection>();
        }
    }
}

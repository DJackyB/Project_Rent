using System;
using System.Collections.Generic;
using System.Linq;

namespace Martian.Tooltip.Presets
{
    [Serializable]
    public sealed class TooltipDocument
    {
        public string Title { get; }
        public string Subtitle { get; }
        public string Summary { get; }
        public IReadOnlyList<string> Tags { get; }
        public IReadOnlyList<TooltipDocumentSection> Sections { get; }

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
            Tags = tags != null
                ? tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray()
                : Array.Empty<string>();
            Sections = sections != null
                ? sections.Where(section => section != null).ToArray()
                : Array.Empty<TooltipDocumentSection>();
        }
    }
}

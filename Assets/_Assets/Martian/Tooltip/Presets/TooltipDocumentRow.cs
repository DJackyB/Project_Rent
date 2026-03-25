using System;

namespace Martian.Tooltip.Presets
{
    [Serializable]
    public sealed class TooltipDocumentRow
    {
        public string Label { get; }
        public string Value { get; }
        public bool Highlight { get; }

        public TooltipDocumentRow(string label, string value, bool highlight = false)
        {
            Label = label;
            Value = value;
            Highlight = highlight;
        }
    }
}

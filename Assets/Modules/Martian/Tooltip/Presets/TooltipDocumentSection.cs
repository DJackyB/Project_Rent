using System;
using System.Collections.Generic;
using System.Linq;

namespace Martian.Tooltip.Presets
{
    [Serializable]
    public sealed class TooltipDocumentSection
    {
        public string Header { get; }
        public IReadOnlyList<TooltipDocumentRow> Rows { get; }

        public TooltipDocumentSection(string header, IEnumerable<TooltipDocumentRow> rows = null)
        {
            Header = header;
            Rows = rows != null
                ? rows.Where(row => row != null).ToArray()
                : Array.Empty<TooltipDocumentRow>();
        }
    }
}

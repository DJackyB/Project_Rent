namespace BaoZuPo.UI.Common.Tooltip
{
    public enum TooltipContentKind
    {
        Card = 0
    }

    public sealed class TooltipContent
    {
        public TooltipContentKind Kind { get; }
        public object Payload { get; }

        public TooltipContent(TooltipContentKind kind, object payload)
        {
            Kind = kind;
            Payload = payload;
        }
    }
}

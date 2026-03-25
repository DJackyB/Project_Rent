namespace Martian.Tooltip
{
    public sealed class TooltipContent
    {
        public string ContentId { get; }
        public object Payload { get; }
        public string DebugLabel { get; }

        public TooltipContent(string contentId, object payload, string debugLabel = null)
        {
            ContentId = string.IsNullOrWhiteSpace(contentId) ? "default" : contentId;
            Payload = payload;
            DebugLabel = debugLabel;
        }
    }
}

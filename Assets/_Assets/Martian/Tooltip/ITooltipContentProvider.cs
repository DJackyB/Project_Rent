namespace Martian.Tooltip
{
    public interface ITooltipContentProvider
    {
        bool TryBuildTooltipRequest(out TooltipRequest request);
    }
}

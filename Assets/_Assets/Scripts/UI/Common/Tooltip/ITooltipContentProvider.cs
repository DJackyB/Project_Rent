namespace BaoZuPo.UI.Common.Tooltip
{
    public interface ITooltipContentProvider
    {
        bool TryBuildTooltipRequest(out TooltipRequest request);
    }
}

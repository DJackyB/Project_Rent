using BaoZuPo.UI.Settlement;

namespace BaoZuPo.GameFlow
{
    public interface ISettlementPresentationMapper
    {
        UISettlementPlaybackBatch Map(SettlementResult result);
    }
}

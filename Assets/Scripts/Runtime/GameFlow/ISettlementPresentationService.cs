using System.Threading;
using BaoZuPo.UI.Settlement;
using Cysharp.Threading.Tasks;

namespace BaoZuPo.GameFlow
{
    public interface ISettlementPresentationService
    {
        UniTask PlayAsync(UISettlementPlaybackBatch batch, CancellationToken cancellationToken = default);
    }
}

using System.Threading;
using BaoZuPo.UI;
using BaoZuPo.UI.Settlement;
using Cysharp.Threading.Tasks;

namespace BaoZuPo.GameFlow
{
    public sealed class SettlementPresentationService : ISettlementPresentationService
    {
        public UniTask PlayAsync(UISettlementPlaybackBatch batch, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            UIManager.Instance?.SubmitSettlementBatch(batch);
            return UniTask.CompletedTask;
        }
    }
}

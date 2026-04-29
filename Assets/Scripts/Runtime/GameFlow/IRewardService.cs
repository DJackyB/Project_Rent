using System.Threading;
using Cysharp.Threading.Tasks;

namespace BaoZuPo.GameFlow
{
    public interface IRewardService
    {
        UniTask<RewardChoiceResult> OfferAndWaitChoiceAsync(bool boosted, CancellationToken ct);
    }
}

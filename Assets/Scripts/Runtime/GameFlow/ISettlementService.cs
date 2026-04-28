using System.Threading;
using Cysharp.Threading.Tasks;

namespace BaoZuPo.GameFlow
{
    public interface ISettlementService
    {
        UniTask<SettlementResult> ResolveAsync(SettlementRequest request, CancellationToken cancellationToken = default);
    }
}

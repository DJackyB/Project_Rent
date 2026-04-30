using System.Threading;
using Cysharp.Threading.Tasks;

namespace BaoZuPo.GameFlow
{
    public interface ITurnFlowService
    {
        UniTask RunAsync(CancellationToken ct);
    }
}

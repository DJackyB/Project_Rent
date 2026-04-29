using System.Threading;
using Cysharp.Threading.Tasks;

namespace BaoZuPo.GameFlow
{
    public interface IShopService
    {
        bool IsOpen { get; }
        bool OpenedThisTurn { get; }
        bool ClosedThisTurn { get; }
        void Open();
        bool TryPurchase(int offerIndex);
        void Close(int turnNumber);
        void ResetForNewTurn();
        UniTask OpenAndWaitCloseAsync(CancellationToken ct);
    }
}

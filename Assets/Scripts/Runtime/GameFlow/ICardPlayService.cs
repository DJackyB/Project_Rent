using System.Threading;
using BaoZuPo.Board;
using BaoZuPo.Card;
using Cysharp.Threading.Tasks;

namespace BaoZuPo.GameFlow
{
    public interface ICardPlayService
    {
        CardPlayTargetKind GetRequiredTargetKind(CardInstance card);

        CardPlayValidationResult ValidatePlay(CardInstance card, RoomSlot targetRoom = null);

        CardPlayResult Play(CardInstance card, RoomSlot targetRoom = null);

        UniTask<CardPlayResult> PlayAsync(
            CardInstance card,
            RoomSlot targetRoom = null,
            CancellationToken cancellationToken = default);
    }
}

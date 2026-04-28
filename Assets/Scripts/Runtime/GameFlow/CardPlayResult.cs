using BaoZuPo.Board;
using BaoZuPo.Card;

namespace BaoZuPo.GameFlow
{
    public readonly struct CardPlayResult
    {
        private CardPlayResult(
            bool succeeded,
            CardInstance card,
            RoomSlot targetRoom,
            CardPlayValidationResult validation)
        {
            Succeeded = succeeded;
            Card = card;
            TargetRoom = targetRoom;
            Validation = validation;
        }

        public bool Succeeded { get; }
        public CardInstance Card { get; }
        public RoomSlot TargetRoom { get; }
        public CardPlayValidationResult Validation { get; }
        public CardPlayBlockReason BlockReason => Validation.BlockReason;

        public static CardPlayResult Success(CardInstance card, CardPlayValidationResult validation)
        {
            return new CardPlayResult(true, card, validation.TargetRoom, validation);
        }

        public static CardPlayResult Failure(CardInstance card, CardPlayValidationResult validation)
        {
            return new CardPlayResult(false, card, validation.TargetRoom, validation);
        }
    }
}

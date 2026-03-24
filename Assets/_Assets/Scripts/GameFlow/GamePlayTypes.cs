using BaoZuPo.Board;

namespace BaoZuPo.GameFlow
{
    public enum GamePhase
    {
        Prepare,
        Action,
        Settle
    }

    public enum CardPlayTargetKind
    {
        PlayArea,
        Room
    }

    public enum CardPlayBlockReason
    {
        None,
        GameOver,
        NotActionPhase,
        InsufficientMoney,
        MissingTarget,
        InvalidTarget,
        TargetFull
    }

    public readonly struct CardPlayValidationResult
    {
        public bool IsValid { get; }
        public CardPlayTargetKind RequiredTargetKind { get; }
        public CardPlayBlockReason BlockReason { get; }
        public RoomSlot TargetRoom { get; }

        public CardPlayValidationResult(
            bool isValid,
            CardPlayTargetKind requiredTargetKind,
            CardPlayBlockReason blockReason,
            RoomSlot targetRoom)
        {
            IsValid = isValid;
            RequiredTargetKind = requiredTargetKind;
            BlockReason = blockReason;
            TargetRoom = targetRoom;
        }

        public static CardPlayValidationResult Success(CardPlayTargetKind requiredTargetKind, RoomSlot targetRoom)
        {
            return new CardPlayValidationResult(true, requiredTargetKind, CardPlayBlockReason.None, targetRoom);
        }

        public static CardPlayValidationResult Failure(
            CardPlayBlockReason blockReason,
            CardPlayTargetKind requiredTargetKind,
            RoomSlot targetRoom = null)
        {
            return new CardPlayValidationResult(false, requiredTargetKind, blockReason, targetRoom);
        }
    }
}

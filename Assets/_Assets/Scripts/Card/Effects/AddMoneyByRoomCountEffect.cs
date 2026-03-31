using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    public class AddMoneyByRoomCountEffect : ICardEffect
    {
        private readonly int _amountPerRoom;

        public AddMoneyByRoomCountEffect(int amountPerRoom)
        {
            _amountPerRoom = amountPerRoom;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            int roomCount = context.BoardManager.RoomCount;
            int total = roomCount * _amountPerRoom;
            if (total != 0)
            {
                context.MoneyManager.AddMoney(total);
                context?.SettlementCapture?.RecordDelta(total, source != null && source.Data != null ? source.Data.cardName : null);
            }

            Debug.Log($"[MirrorEffect] {source.Data.cardName}: {roomCount} room(s), money delta {total}");
        }
    }
}

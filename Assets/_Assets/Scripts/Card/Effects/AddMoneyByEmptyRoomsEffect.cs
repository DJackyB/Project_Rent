using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    public class AddMoneyByEmptyRoomsEffect : ICardEffect
    {
        private readonly int _amountPerRoom;

        public AddMoneyByEmptyRoomsEffect(int amountPerRoom)
        {
            _amountPerRoom = amountPerRoom;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            int emptyCount = 0;
            foreach (var room in context.BoardManager.GetAllRooms())
            {
                if (room.TenantCount <= 0)
                {
                    emptyCount++;
                }
            }

            int total = emptyCount * _amountPerRoom;
            if (total != 0)
            {
                context.MoneyManager.AddMoney(total);
                context?.SettlementCapture?.RecordDelta(total, source != null && source.Data != null ? source.Data.cardName : null);
            }

            Debug.Log($"[MirrorEffect] {source.Data.cardName}: {emptyCount} empty room(s), money delta {total}");
        }
    }
}

using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// Add money based on current room count.
    /// </summary>
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
                context?.SettlementCapture?.RecordDelta(total);
            }

            Debug.Log($"[镜像效果] {source.Data.cardName}: 房间 {roomCount} 间，资金变化 {total}");
        }
    }
}

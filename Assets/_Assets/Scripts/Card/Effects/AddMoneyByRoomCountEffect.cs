using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 按房间总数加钱
    /// 格式：AddMoneyByRoomCount;每个房间金额
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
            }

            Debug.Log($"[效果] {source.Data.cardName}: 房间 {roomCount} 间，资金变化 {total}");
        }
    }
}

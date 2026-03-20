using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 按空房间数量加钱
    /// 格式：AddMoneyByEmptyRooms;每个空房金额
    /// </summary>
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
                    emptyCount++;
            }

            int total = emptyCount * _amountPerRoom;
            if (total != 0)
            {
                context.MoneyManager.AddMoney(total);
            }

            Debug.Log($"[效果] {source.Data.cardName}: 空房 {emptyCount} 间，资金变化 {total}");
        }
    }
}

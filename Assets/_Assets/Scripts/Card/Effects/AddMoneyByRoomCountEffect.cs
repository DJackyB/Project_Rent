using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 按当前房间总数计算增加的金额。
    /// </summary>
    public class AddMoneyByRoomCountEffect : ICardEffect
    {
        private readonly int _amountPerRoom;

        public AddMoneyByRoomCountEffect(int amountPerRoom)
        {
            _amountPerRoom = amountPerRoom;
        }

        // 执行：获取棋盘房间总数，乘以 _amountPerRoom 得出总额，然后增加金钱。
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

using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 按空房间总数计算增加的金额。
    /// </summary>
    public class AddMoneyByEmptyRoomsEffect : ICardEffect
    {
        private readonly int _amountPerRoom;

        public AddMoneyByEmptyRoomsEffect(int amountPerRoom)
        {
            _amountPerRoom = amountPerRoom;
        }

        // 执行：遍历所有房间，统计租客数为 0 的空房间，乘以 _amountPerRoom 得出总额，然后增加金钱。
        public void Execute(CardInstance source, GameContext context)
        {
            int emptyCount = context.BoardManager.GetEmptyRoomCount();
            int total = emptyCount * _amountPerRoom;
            if (total != 0)
            {
                context.MoneyManager.AddMoney(total);
                context?.SettlementCapture?.RecordDelta(total, source != null && source.Data != null ? source.Data.cardName : null);
            }

            Debug.Log($"[MirrorEffect] {EffectSourceHelper.Name(source)}: {emptyCount} empty room(s), money delta {total}");
        }
    }
}

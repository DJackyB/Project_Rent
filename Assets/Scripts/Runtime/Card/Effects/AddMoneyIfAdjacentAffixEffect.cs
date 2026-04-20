using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 若卡牌所在房间的任一相邻房间有指定词条的租客，增加固定金额。
    /// 格式：AddMoneyIfAdjacentAffix;Quiet;200
    /// </summary>
    public class AddMoneyIfAdjacentAffixEffect : ICardEffect
    {
        private readonly TagType _affix;
        private readonly int _amount;

        public AddMoneyIfAdjacentAffixEffect(TagType affix, int amount)
        {
            _affix = affix;
            _amount = amount;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            var room = source?.PlacedRoom;
            if (room == null)
            {
                return;
            }

            var rooms = context.BoardManager.GetAllRooms();
            var left = TagQuery.GetAdjacentRoom(room, -1, rooms);
            var right = TagQuery.GetAdjacentRoom(room, 1, rooms);

            bool hasAdjacentAffix = (left != null && TagQuery.RoomHasTag(left, _affix))
                || (right != null && TagQuery.RoomHasTag(right, _affix));

            if (!hasAdjacentAffix)
            {
                return;
            }

            context.MoneyManager.AddMoney(_amount);
            if (context.SettlementCapture.IsCapturing)
            {
                context.SettlementCapture.RecordDelta(_amount, source.Data?.cardName);
            }

            Debug.Log($"[Effect] {source.Data?.cardName}: AddMoneyIfAdjacentAffix({_affix}, {_amount})");
        }
    }
}

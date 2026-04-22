using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 若卡牌所在房间的所有相邻房间都有指定词条的租客（边缘房间仅要求存在的邻居满足），
    /// 对该房间词条租客的 baseRent 总和施加乘值，追加差额收益。
    /// V1 简化：乘区基数仅计算词条租客 baseRent，不含装备加成。
    /// 格式：MultiplyIfAllAdjacentAffix;Quiet;2.0
    /// </summary>
    public class MultiplyIfAllAdjacentAffixEffect : ICardEffect
    {
        private readonly TagType _affix;
        private readonly float _multiplier;

        public MultiplyIfAllAdjacentAffixEffect(TagType affix, float multiplier)
        {
            _affix = affix;
            _multiplier = multiplier;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            var room = source?.PlacedRoom;
            if (room == null)
            {
                return;
            }

            var rooms = context.BoardManager.GetAllRooms();
            if (!TagQuery.AllAdjacentRoomsHaveTag(room, _affix, rooms))
            {
                return;
            }

            int baseRentSum = TagQuery.SumBaseRentForTag(room, _affix);
            int bonus = Mathf.RoundToInt(baseRentSum * (_multiplier - 1f));
            if (bonus <= 0)
            {
                return;
            }

            context.MoneyManager.AddMoney(bonus);
            if (context.SettlementCapture.IsCapturing)
            {
                context.SettlementCapture.RecordDelta(bonus, EffectSourceHelper.Name(source));
            }

            Debug.Log($"[Effect] {EffectSourceHelper.Name(source)}: MultiplyIfAllAdjacentAffix({_affix}, x{_multiplier}) +{bonus}");
        }
    }
}

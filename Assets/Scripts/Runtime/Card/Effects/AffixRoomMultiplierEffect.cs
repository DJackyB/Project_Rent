using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 遍历所有房间，对含有指定词条租客的房间，基于该房间词条租客 baseRent 总和施加乘值，追加差额收益。
    /// V1 简化：乘区基数仅计算词条租客 baseRent，不含装备加成。
    /// 格式：AffixRoomMultiplier;Quiet;1.5
    /// </summary>
    public class AffixRoomMultiplierEffect : ICardEffect
    {
        private readonly TagType _affix;
        private readonly float _multiplier;

        public AffixRoomMultiplierEffect(TagType affix, float multiplier)
        {
            _affix = affix;
            _multiplier = multiplier;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            var rooms = context.BoardManager.GetAllRooms();
            int totalBonus = 0;

            foreach (var room in rooms)
            {
                if (!TagQuery.RoomHasTag(room, _affix))
                {
                    continue;
                }

                int baseRentSum = TagQuery.SumBaseRentForTag(room, _affix);
                int bonus = Mathf.RoundToInt(baseRentSum * (_multiplier - 1f));
                if (bonus > 0)
                {
                    totalBonus += bonus;
                }
            }

            if (totalBonus <= 0)
            {
                return;
            }

            context.MoneyManager.AddMoney(totalBonus);
            if (context.SettlementCapture.IsCapturing)
            {
                context.SettlementCapture.RecordDelta(totalBonus, source.Data?.cardName);
            }

            Debug.Log($"[Effect] {source.Data?.cardName}: AffixRoomMultiplier({_affix}, x{_multiplier}) +{totalBonus}");
        }
    }
}

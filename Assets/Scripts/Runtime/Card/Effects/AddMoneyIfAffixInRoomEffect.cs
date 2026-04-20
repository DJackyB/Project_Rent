using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 若卡牌所在房间有指定词条的租客，增加固定金额。
    /// 格式：AddMoneyIfAffixInRoom;Quiet;200
    /// </summary>
    public class AddMoneyIfAffixInRoomEffect : ICardEffect
    {
        private readonly TagType _affix;
        private readonly int _amount;

        public AddMoneyIfAffixInRoomEffect(TagType affix, int amount)
        {
            _affix = affix;
            _amount = amount;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            var room = source?.PlacedRoom;
            if (room == null || !TagQuery.RoomHasTag(room, _affix))
            {
                return;
            }

            context.MoneyManager.AddMoney(_amount);
            if (context.SettlementCapture.IsCapturing)
            {
                context.SettlementCapture.RecordDelta(_amount, source.Data?.cardName);
            }

            Debug.Log($"[Effect] {source.Data?.cardName}: AddMoneyIfAffixInRoom({_affix}, {_amount})");
        }
    }
}

using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 统计场上所有拥有指定词条的租客数量，每人增加固定金额。
    /// 格式：AddMoneyPerAffixOnBoard;Quiet;200
    /// </summary>
    public class AddMoneyPerAffixOnBoardEffect : ICardEffect
    {
        private readonly TagType _affix;
        private readonly int _amountPerTenant;

        public AddMoneyPerAffixOnBoardEffect(TagType affix, int amountPerTenant)
        {
            _affix = affix;
            _amountPerTenant = amountPerTenant;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            var rooms = context.BoardManager.GetAllRooms();
            int count = TagQuery.CountTagOnBoard(rooms, _affix);
            if (count <= 0)
            {
                return;
            }

            int total = count * _amountPerTenant;
            context.MoneyManager.AddMoney(total);
            if (context.SettlementCapture.IsCapturing)
            {
                context.SettlementCapture.RecordDelta(total, source.Data?.cardName);
            }

            Debug.Log($"[Effect] {source.Data?.cardName}: AddMoneyPerAffixOnBoard({_affix}) x{count} = +{total}");
        }
    }
}

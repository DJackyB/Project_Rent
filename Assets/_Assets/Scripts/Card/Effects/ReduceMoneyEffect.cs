using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    public class ReduceMoneyEffect : ICardEffect
    {
        private readonly int _amount;

        public ReduceMoneyEffect(int amount)
        {
            _amount = amount;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            bool success = context.MoneyManager.ReduceMoney(_amount);
            if (success)
            {
                context?.SettlementCapture?.RecordDelta(-_amount, source != null && source.Data != null ? source.Data.cardName : null);
            }

            Debug.Log($"[MirrorEffect] {source.Data.cardName} triggered ReduceMoney({_amount}), success: {success}");
        }
    }
}

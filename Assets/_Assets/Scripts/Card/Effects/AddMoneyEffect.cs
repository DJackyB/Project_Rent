using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    public class AddMoneyEffect : ICardEffect
    {
        private readonly int _amount;

        public AddMoneyEffect(int amount)
        {
            _amount = amount;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            context.MoneyManager.AddMoney(_amount);
            context?.SettlementCapture?.RecordDelta(_amount, source != null && source.Data != null ? source.Data.cardName : null);
            Debug.Log($"[MirrorEffect] {source.Data.cardName} triggered AddMoney({_amount})");
        }
    }
}

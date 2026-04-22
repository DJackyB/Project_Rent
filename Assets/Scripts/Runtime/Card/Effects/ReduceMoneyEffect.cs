using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 直接扣除指定金额。
    /// </summary>
    public class ReduceMoneyEffect : ICardEffect
    {
        private readonly int _amount;

        public ReduceMoneyEffect(int amount)
        {
            _amount = amount;
        }

        // 执行：尝试扣除 _amount，仅在成功时记录结算变化（失败时不记录）。
        public void Execute(CardInstance source, GameContext context)
        {
            bool success = context.MoneyManager.ReduceMoney(_amount);
            if (success)
            {
                context?.SettlementCapture?.RecordDelta(-_amount, source != null && source.Data != null ? source.Data.cardName : null);
            }

            Debug.Log($"[MirrorEffect] {EffectSourceHelper.Name(source)} triggered ReduceMoney({_amount}), success: {success}");
        }
    }
}

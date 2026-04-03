using BaoZuPo.Board;
using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 为所有棋盘上的租客增加耐久。
    /// </summary>
    public class AddTenantDurabilityEffect : ICardEffect
    {
        private readonly int _amount;

        public AddTenantDurabilityEffect(int amount)
        {
            _amount = amount;
        }

        // 执行：遍历所有房间的所有租客，对未销毁且具有耐久属性（durability > 0）的租客增加耐久值。
        public void Execute(CardInstance card, GameContext context)
        {
            int count = 0;
            var rooms = context.BoardManager.GetAllRooms();
            foreach (var room in rooms)
            {
                foreach (var tenant in room.GetTenants())
                {
                    if (!tenant.IsDestroyed && tenant.Data.durability > 0)
                    {
                        tenant.CurrentDurability += _amount;
                        count++;
                    }
                }
            }

            Debug.Log($"[Effect] Renew lease: {count} tenant card(s) durability +{_amount}");
        }
    }
}

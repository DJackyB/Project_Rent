using UnityEngine;
using BaoZuPo.Card;
using BaoZuPo.Board;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 给场上所有租客增加耐久（续租）
    /// 格式：AddTenantDurability;数量
    /// </summary>
    public class AddTenantDurabilityEffect : ICardEffect
    {
        private readonly int _amount;

        public AddTenantDurabilityEffect(int amount)
        {
            _amount = amount;
        }

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
            Debug.Log($"[效果] 续租：{count} 个租客耐久 +{_amount}");
        }
    }
}

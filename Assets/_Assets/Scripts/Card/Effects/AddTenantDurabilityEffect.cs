using BaoZuPo.Board;
using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
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

            Debug.Log($"[Effect] Renew lease: {count} tenant card(s) durability +{_amount}");
        }
    }
}

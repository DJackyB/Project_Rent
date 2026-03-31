using BaoZuPo.Board;
using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    public class AddEquipmentDurabilityEffect : ICardEffect
    {
        private readonly int _amount;

        public AddEquipmentDurabilityEffect(int amount)
        {
            _amount = amount;
        }

        public void Execute(CardInstance card, GameContext context)
        {
            int count = 0;
            var rooms = context.BoardManager.GetAllRooms();
            foreach (var room in rooms)
            {
                foreach (var equip in room.GetEquipments())
                {
                    if (!equip.IsDestroyed && equip.Data.durability > 0)
                    {
                        equip.CurrentDurability += _amount;
                        count++;
                    }
                }
            }

            Debug.Log($"[Effect] Repair: {count} equipment card(s) durability +{_amount}");
        }
    }
}

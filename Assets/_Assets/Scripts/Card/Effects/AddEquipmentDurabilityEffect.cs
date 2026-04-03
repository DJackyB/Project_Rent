using BaoZuPo.Board;
using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 为所有棋盘上的装备增加耐久。
    /// </summary>
    public class AddEquipmentDurabilityEffect : ICardEffect
    {
        private readonly int _amount;

        public AddEquipmentDurabilityEffect(int amount)
        {
            _amount = amount;
        }

        // 执行：遍历所有房间的所有装备，对未销毁且具有耐久属性（durability > 0）的装备增加耐久值。
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

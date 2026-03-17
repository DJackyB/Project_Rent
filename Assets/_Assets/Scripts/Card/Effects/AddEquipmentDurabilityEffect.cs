using UnityEngine;
using BaoZuPo.Card;
using BaoZuPo.Board;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 给场上所有装备增加耐久（维修）
    /// 格式：AddEquipmentDurability;数量
    /// </summary>
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
            Debug.Log($"[效果] 维修：{count} 个装备耐久 +{_amount}");
        }
    }
}

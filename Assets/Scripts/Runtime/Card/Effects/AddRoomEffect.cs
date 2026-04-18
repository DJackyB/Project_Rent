using BaoZuPo.Core;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 增加一个新房间。
    /// 格式：AddRoom
    /// </summary>
    public class AddRoomEffect : ICardEffect
    {
        // 执行：从 GameConfig 读取房间默认配置（租客槽位数和装备槽位数），然后调用 BoardManager.AddRoom 创建新房间。
        public void Execute(CardInstance source, GameContext context)
        {
            if (context?.BoardManager == null)
            {
                Debug.LogWarning($"[Effect] {source?.Data?.cardName ?? "Unknown"}: BoardManager is unavailable, AddRoom skipped.");
                return;
            }

            int tenantSlots = 1;
            int equipmentSlots = 3;

            if (GameManager.Instance != null && GameManager.Instance.gameConfig != null)
            {
                tenantSlots = Mathf.Max(1, GameManager.Instance.gameConfig.defaultTenantSlots);
                equipmentSlots = Mathf.Max(0, GameManager.Instance.gameConfig.defaultEquipmentSlots);
            }

            var room = context.BoardManager.AddRoom(tenantSlots, equipmentSlots);
            Debug.Log($"[Effect] {source?.Data?.cardName ?? "Unknown"}: Added room {room.RoomIndex}.");
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BaoZuPo.Board;
using BaoZuPo.Card;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 单个房间视图（Prefab 脚本）
    /// 显示房间信息和其中的卡牌，点击放入选中的手牌。
    /// </summary>
    public class UIRoomView : MonoBehaviour
    {
        [Header("UI 引用")]
        public TextMeshProUGUI titleText;
        public Transform cardListContainer;
        public Button roomButton;

        private RoomSlot _room;
        private UIBoardPanel _boardPanel;

        public void Setup(RoomSlot room, GameObject cardEntryPrefab, UIBoardPanel boardPanel)
        {
            _room = room;
            _boardPanel = boardPanel;

            // 标题
            if (titleText != null)
            {
                titleText.text = $"Room {room.RoomIndex} ({room.TenantCount} Tenants / {room.EquipmentCount} Equips)";
            }

            // 显示房间内的卡牌
            if (cardListContainer != null && cardEntryPrefab != null)
            {
                foreach (var card in room.GetAllCards())
                {
                    var go = Instantiate(cardEntryPrefab, cardListContainer);
                    var text = go.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        string typeTag = card.Data.cardType == CardType.Tenant ? "🏠" : "🔧";
                        string durInfo = card.Data.durability > 0 ? $" Durability:{card.CurrentDurability}" : "";
                        text.text = $"{typeTag} {card.Data.cardName}{durInfo}";
                    }
                }
            }

            // 点击事件
            if (roomButton != null)
            {
                roomButton.onClick.RemoveAllListeners();
                roomButton.onClick.AddListener(() => _boardPanel.TryPlaceCard(_room));
            }
        }
    }
}

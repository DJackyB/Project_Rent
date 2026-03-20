using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.GameFlow;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 房间/棋盘面板
    /// 显示所有房间及其中的卡牌。点击房间 = 放置选中的手牌。
    /// </summary>
    public class UIBoardPanel : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("房间 UI Prefab")]
        public GameObject roomPrefab;

        [Tooltip("房间内卡牌条目 Prefab（简单的文本条目）")]
        public GameObject roomCardEntryPrefab;

        [Tooltip("房间容器")]
        public Transform roomContainer;

        private List<UIRoomView> _roomViews = new();

        /// <summary>
        /// 刷新棋盘显示
        /// </summary>
        public void RefreshBoard()
        {
            var rooms = BoardManager.Instance.GetAllRooms();

            // Destroy ALL children of the container (to remove placeholders)
            foreach (Transform child in roomContainer)
            {
                Destroy(child.gameObject);
            }
            _roomViews.Clear();

            // Spawn new UI elements
            for (int i = 0; i < rooms.Count; i++)
            {
                if (roomPrefab == null)
                {
                    Debug.LogError("[UIBoardPanel] roomPrefab 未在 Inspector 中赋值！");
                    break;
                }
                
                var go = Instantiate(roomPrefab, roomContainer);
                var roomView = go.GetComponent<UIRoomView>();
                if (roomView != null)
                {
                    roomView.Setup(rooms[i], roomCardEntryPrefab, this);
                    _roomViews.Add(roomView);
                }
                else
                {
                    Debug.LogError("[UIBoardPanel] roomPrefab 缺少 UIRoomView 组件！");
                }
            }
            
        }

        /// <summary>
        /// 尝试将选中的手牌放入指定房间（由 UIRoomView 调用）
        /// </summary>
        public void TryPlaceCard(RoomSlot room)
        {
            var handPanel = UIManager.Instance.handPanel;
            var card = handPanel?.SelectedCard;

            if (card == null)
            {
                Debug.LogWarning("[UI] Please select a card first.");
                return;
            }

            bool success = TurnManager.Instance.PlayCard(card, room);
            if (success)
            {
                handPanel.DeselectCard();
                UIManager.Instance.RefreshAll();
            }
        }
    }
}

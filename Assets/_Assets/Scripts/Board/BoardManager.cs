using System.Collections.Generic;
using UnityEngine;
using BaoZuPo.Core;
using BaoZuPo.Card;

namespace BaoZuPo.Board
{
    /// <summary>
    /// 棋盘管理器
    /// 管理所有房间的创建、查询，以及遍历场上所有卡牌。
    /// </summary>
    public class BoardManager : Singleton<BoardManager>
    {
        [Header("调试信息")]
        [SerializeField] private List<RoomSlot> _rooms = new();
        [SerializeField] private List<CardInstance> _contracts = new();
        private Transform _roomRoot;

        /// <summary>当前房间数量</summary>
        public int RoomCount => _rooms.Count;

        /// <summary>
        /// 初始化棋盘（由 GameManager 调用）
        /// </summary>
        public void Initialize(int roomCount, int tenantSlots, int equipmentSlots)
        {
            if (_roomRoot == null)
            {
                var go = new GameObject("Rooms");
                _roomRoot = go.transform;
            }

            ClearAllRooms();
            _contracts.Clear();

            for (int i = 0; i < roomCount; i++)
            {
                AddRoom(tenantSlots, equipmentSlots);
            }

            Debug.Log($"[BoardManager] 初始化完成，创建了 {roomCount} 个房间");
        }

        /// <summary>
        /// 动态新增一个房间
        /// </summary>
        public RoomSlot AddRoom(int tenantSlots = 1, int equipmentSlots = 3)
        {
            var roomGO = new GameObject($"Room_{_rooms.Count}");
            roomGO.transform.SetParent(_roomRoot);

            var room = roomGO.AddComponent<RoomSlot>();
            room.Initialize(_rooms.Count, tenantSlots, equipmentSlots);
            _rooms.Add(room);

            Debug.Log($"[BoardManager] 新增房间: {room.RoomIndex}");
            return room;
        }

        /// <summary>
        /// 获取指定索引的房间
        /// </summary>
        public RoomSlot GetRoom(int index)
        {
            if (index < 0 || index >= _rooms.Count)
            {
                Debug.LogError($"[BoardManager] 房间索引越界: {index}");
                return null;
            }
            return _rooms[index];
        }

        /// <summary>
        /// 获取所有房间
        /// </summary>
        public IReadOnlyList<RoomSlot> GetAllRooms() => _rooms;

        /// <summary>
        /// 查找第一个能放置指定类型卡牌的房间
        /// </summary>
        public RoomSlot FindAvailableRoom(CardType cardType)
        {
            foreach (var room in _rooms)
            {
                switch (cardType)
                {
                    case CardType.Tenant when room.CanPlaceTenant:
                        return room;
                    case CardType.Equipment when room.CanPlaceEquipment:
                        return room;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取场上所有卡牌（房间卡 + 合同卡）
        /// </summary>
        public List<CardInstance> GetAllFieldCards()
        {
            var allCards = new List<CardInstance>();
            foreach (var room in _rooms)
            {
                allCards.AddRange(room.GetAllCards());
            }
            allCards.AddRange(_contracts);
            return allCards;
        }

        /// <summary>
        /// 新增一张合同牌（不占用房间）
        /// </summary>
        public void AddContract(CardInstance contract)
        {
            if (contract == null || contract.IsDestroyed) return;
            _contracts.Add(contract);
        }

        /// <summary>
        /// 获取所有已生效的合同牌
        /// </summary>
        public IReadOnlyList<CardInstance> GetAllContracts() => _contracts;

        /// <summary>
        /// 清理所有房间中已销毁的卡牌
        /// </summary>
        public void CleanupDestroyedCards()
        {
            foreach (var room in _rooms)
            {
                room.CleanupDestroyedCards();
            }
            _contracts.RemoveAll(c => c == null || c.IsDestroyed);
        }

        /// <summary>
        /// 清除所有房间
        /// </summary>
        private void ClearAllRooms()
        {
            foreach (var room in _rooms)
            {
                if (room != null)
                    Destroy(room.gameObject);
            }
            _rooms.Clear();
        }
    }
}

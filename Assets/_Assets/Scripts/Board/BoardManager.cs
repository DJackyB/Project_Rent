using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.GameFlow;
using UnityEngine;

namespace BaoZuPo.Board
{
    /// <summary>
    /// 房间管理器：负责创建、销毁、查询和维护所有房间对象。
    ///
    /// 职责：
    /// 1. 房间池管理：初始化时创建指定数量的房间（RoomSlot），各房间维护自己的住户和装备插槽
    /// 2. 房间索引维护：通过整数索引快速查询房间（O(1) 检索）
    /// 3. 房间查询接口：提供按索引查询、查询所有房间、查询可用房间等操作
    /// 4. 契约卡管理：维护放置在房间外的契约卡列表（例如持久化事件卡）
    /// 5. 清理机制：移除被摧毁的卡牌以保持引用有效
    ///
    /// 核心流程：
    /// [初始化] → Initialize(roomCount, tenantSlots, equipmentSlots)
    ///   └─ 清空旧房间 → 逐个AddRoom() → 记录房间引用
    /// [查询] → GetRoom(index) / FindAvailableRoom(cardData) 等
    /// [清理] → CleanupDestroyedCards() 移除失效卡牌
    /// </summary>
    public class BoardManager : Singleton<BoardManager>
    {
        [Header("Debug Info")]
        [SerializeField] private List<RoomSlot> _rooms = new();      // 所有房间的列表，用整数索引快速访问
        [SerializeField] private List<CardInstance> _contracts = new();  // 契约卡列表（不放在房间内的卡牌）

        [Header("Scene References")]
        [SerializeField] private Transform _roomRoot;  // 房间GameObject的父物体，所有房间都创建为其子物体

        public int RoomCount => _rooms.Count;              // 当前房间数量
        public int ContractCount => _contracts.Count;      // 当前契约卡数量

        /// <summary>
        /// 初始化房间池。清空现有房间，创建指定数量的新房间。
        /// 每个房间将拥有相同的住户和装备插槽容量。
        /// </summary>
        /// <param name="roomCount">要创建的房间数量</param>
        /// <param name="tenantSlots">每个房间的住户插槽数（默认1）</param>
        /// <param name="equipmentSlots">每个房间的装备插槽数（默认3）</param>
        public void Initialize(int roomCount, int tenantSlots, int equipmentSlots)
        {
            if (_roomRoot == null)
            {
                Debug.LogError("[BoardManager] _roomRoot is not assigned in the Inspector. Create an empty GameObject named 'Rooms' in the scene and assign it.");
                return;
            }

            ClearAllRooms();        // 清空旧房间列表和对应的GameObject
            _contracts.Clear();     // 清空契约卡列表

            // 创建roomCount个新房间，按顺序添加到_rooms列表
            for (int i = 0; i < roomCount; i++)
            {
                AddRoom(tenantSlots, equipmentSlots);
            }

            Debug.Log($"[BoardManager] Initialized {roomCount} rooms.");
        }

        /// <summary>
        /// 创建并添加一个新房间。
        /// [伪代码]
        /// 1. 创建新GameObject，命名为"Room_X"（X为当前房间总数）
        /// 2. 设置GameObject的父物体为_roomRoot
        /// 3. 添加RoomSlot组件到GameObject
        /// 4. 初始化RoomSlot，传递房间索引和插槽容量
        /// 5. 将房间引用添加到_rooms列表
        /// 6. 返回新创建的房间实例
        /// </summary>
        /// <param name="tenantSlots">该房间的住户插槽数</param>
        /// <param name="equipmentSlots">该房间的装备插槽数</param>
        /// <returns>新创建的房间实例</returns>
        public RoomSlot AddRoom(int tenantSlots = 1, int equipmentSlots = 3)
        {
            var roomGO = new GameObject($"Room_{_rooms.Count}");
            roomGO.transform.SetParent(_roomRoot);

            var room = roomGO.AddComponent<RoomSlot>();
            room.Initialize(_rooms.Count, tenantSlots, equipmentSlots);  // 房间索引 = 当前列表大小
            _rooms.Add(room);

            Debug.Log($"[BoardManager] Added room: {room.RoomIndex}");
            return room;
        }

        /// <summary>
        /// 按索引获取房间。性能特点：O(1) 列表索引访问。
        /// </summary>
        /// <param name="index">房间索引（从0开始）</param>
        /// <returns>房间实例，如果索引越界则返回null并打印错误</returns>
        public RoomSlot GetRoom(int index)
        {
            if (index < 0 || index >= _rooms.Count)
            {
                Debug.LogError($"[BoardManager] Room index out of range: {index}");
                return null;
            }

            return _rooms[index];
        }

        /// <summary>
        /// 获取所有房间的只读列表。用于遍历所有房间执行批量操作。
        /// </summary>
        /// <returns>房间列表的只读视图</returns>
        public IReadOnlyList<RoomSlot> GetAllRooms() => _rooms;

        /// <summary>
        /// 查询适合放置给定卡牌的房间。性能特点：O(n) 遍历所有房间直到找到可用的。
        /// [伪代码]
        /// 1. 验证卡牌是否为null且目标类型为"房间"
        /// 2. 遍历所有房间：
        ///    - 如果卡牌在房间内持久化（住户/装备）：
        ///      └─ 检查对应的插槽是否有空位，有则返回该房间
        ///    - 如果卡牌为一次性卡（事件卡）：
        ///      └─ 任意房间都可以，返回第一个房间
        /// 3. 如果没有找到可用房间，返回null
        /// </summary>
        /// <param name="card">要放置的卡牌数据</param>
        /// <returns>可用房间，如果没有可用房间返回null</returns>
        public RoomSlot FindAvailableRoom(CardData card)
        {
            if (card == null || CardTargeting.GetRequiredTargetKind(card) != CardPlayTargetKind.Room)
            {
                return null;
            }

            foreach (var room in _rooms)
            {
                // 检查卡牌是否需要在房间内持久化存在（住户或装备）
                if (CardTargeting.PersistsInRoom(card))
                {
                    switch (card.cardType)
                    {
                        case CardType.Tenant when room.CanPlaceTenant:
                            return room;  // 找到有空的住户插槽的房间
                        case CardType.Equipment when room.CanPlaceEquipment:
                            return room;  // 找到有空的装备插槽的房间
                    }

                    continue;  // 该房间没有合适的插槽，继续寻找
                }

                // 一次性卡（事件卡）可以在任何房间执行
                return room;
            }

            return null;  // 没有可用房间
        }

        /// <summary>
        /// 获取棋盘上所有的卡牌（房间内的 + 契约卡）。
        /// 用于遍历全局卡牌状态（例如检查所有卡牌的触发条件）。
        /// </summary>
        /// <returns>包含所有房间卡牌和契约卡的列表</returns>
        public List<CardInstance> GetAllFieldCards()
        {
            var allCards = new List<CardInstance>();

            // 遍历所有房间，收集房间内的住户和装备
            foreach (var room in _rooms)
            {
                allCards.AddRange(room.GetAllCards());
            }

            // 添加契约卡（房间外的持久化卡）
            allCards.AddRange(_contracts);
            return allCards;
        }

        /// <summary>
        /// 将契约卡添加到棋盘。契约卡是放置在房间外的持久化卡（例如持续生效的事件或负面效果）。
        /// </summary>
        /// <param name="contract">要添加的契约卡</param>
        public void AddContract(CardInstance contract)
        {
            if (contract == null || contract.IsDestroyed)
            {
                return;
            }

            _contracts.Add(contract);
        }

        /// <summary>
        /// 获取所有契约卡的只读列表。
        /// </summary>
        /// <returns>契约卡列表的只读视图</returns>
        public IReadOnlyList<CardInstance> GetAllContracts() => _contracts;

        /// <summary>
        /// 按索引获取契约卡。性能特点：O(1)。
        /// </summary>
        /// <param name="index">契约卡索引</param>
        /// <returns>契约卡，如果索引越界返回null</returns>
        public CardInstance GetContractAt(int index)
        {
            return TryGetContract(index, out var contract) ? contract : null;
        }

        /// <summary>
        /// 尝试按索引获取契约卡。性能特点：O(1)。
        /// </summary>
        /// <param name="index">契约卡索引</param>
        /// <param name="contract">如果成功获取，输出卡牌实例；否则为null</param>
        /// <returns>是否成功获取</returns>
        public bool TryGetContract(int index, out CardInstance contract)
        {
            if (index >= 0 && index < _contracts.Count)
            {
                contract = _contracts[index];
                return true;
            }

            contract = null;
            return false;
        }

        /// <summary>
        /// 清理所有被摧毁的卡牌。在回合结束或卡牌被销毁时调用。
        /// [伪代码]
        /// 1. 遍历每个房间，调用其CleanupDestroyedCards()
        /// 2. 从契约卡列表中移除所有满足 (c == null || c.IsDestroyed) 的卡牌
        /// </summary>
        public void CleanupDestroyedCards()
        {
            // 清理各房间内的被摧毁卡牌
            foreach (var room in _rooms)
            {
                room.CleanupDestroyedCards();
            }

            // 清理被摧毁的契约卡
            _contracts.RemoveAll(c => c == null || c.IsDestroyed);
        }

        /// <summary>
        /// 清空所有房间。销毁房间GameObject，清空房间列表。用于重新初始化或场景清理。
        /// </summary>
        private void ClearAllRooms()
        {
            foreach (var room in _rooms)
            {
                if (room != null)
                {
                    Destroy(room.gameObject);  // 销毁房间GameObject及其所有组件
                }
            }

            _rooms.Clear();
        }
    }
}

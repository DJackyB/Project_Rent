using System.Collections.Generic;
using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.Board
{
    /// <summary>
    /// 房间插槽：包租婆游戏的核心单位，管理单个房间的住户和装备。
    ///
    /// 职责：
    /// 1. 住户管理：维护房间内的住户卡牌列表，支持最多_maxTenantSlots个住户
    /// 2. 装备管理：维护房间内的装备卡牌列表，支持最多_maxEquipmentSlots个装备
    /// 3. 结算逻辑：计算房租收入、耐久减少、驱逐条件等（留给逻辑层调用）
    /// 4. 卡牌查询：提供按类型、按索引查询房间内的卡牌
    ///
    /// 核心计算（结算时由外部逻辑调用）：
    /// - 房租收入 = 所有住户的基础房租 + 装备加成
    /// - 耐久减少 = 住户等级相关的耐久成本
    /// - 驱逐条件 = 耐久 <= 0
    /// </summary>
    public class RoomSlot : MonoBehaviour
    {
        [Header("Room Config")]
        [Tooltip("Tenant slot capacity. Default: 1.")]
        [SerializeField] private int _maxTenantSlots = 1;

        [Tooltip("Equipment slot capacity. Default: 3.")]
        [SerializeField] private int _maxEquipmentSlots = 3;

        private readonly List<CardInstance> _tenants = new();      // 房间内的住户卡牌列表
        private readonly List<CardInstance> _equipments = new();   // 房间内的装备卡牌列表

        /// <summary>房间索引（由BoardManager分配，用于唯一标识房间）</summary>
        public int RoomIndex { get; set; }

        /// <summary>住户插槽总容量</summary>
        public int TenantSlotCapacity => _maxTenantSlots;

        /// <summary>装备插槽总容量</summary>
        public int EquipmentSlotCapacity => _maxEquipmentSlots;

        /// <summary>当前房间内的住户数量</summary>
        public int TenantCount => _tenants.Count;

        /// <summary>当前房间内的装备数量</summary>
        public int EquipmentCount => _equipments.Count;

        /// <summary>是否还有空的住户插槽（用于放置新住户时检查）</summary>
        public bool CanPlaceTenant => _tenants.Count < _maxTenantSlots;

        /// <summary>是否还有空的装备插槽（用于放置新装备时检查）</summary>
        public bool CanPlaceEquipment => _equipments.Count < _maxEquipmentSlots;

        /// <summary>
        /// 初始化房间。由BoardManager在创建房间时调用。
        /// </summary>
        /// <param name="index">房间在BoardManager中的索引</param>
        /// <param name="tenantSlots">该房间的住户插槽容量</param>
        /// <param name="equipmentSlots">该房间的装备插槽容量</param>
        public void Initialize(int index, int tenantSlots, int equipmentSlots)
        {
            RoomIndex = index;
            _maxTenantSlots = tenantSlots;
            _maxEquipmentSlots = equipmentSlots;
        }

        /// <summary>
        /// 将卡牌放置到房间。根据卡牌类型（住户/装备）放入相应列表。
        /// [伪代码]
        /// 1. 验证卡牌有效性
        /// 2. 根据卡牌类型：
        ///    - Tenant: 检查住户插槽是否有空位 → 有则添加到_tenants
        ///    - Equipment: 检查装备插槽是否有空位 → 有则添加到_equipments
        ///    - 其他: 拒绝放置，返回false
        /// 3. 设置卡牌的PlacedRoom引用指向本房间
        /// 4. 返回true表示放置成功
        /// </summary>
        /// <param name="card">要放置的卡牌实例</param>
        /// <returns>是否成功放置</returns>
        public bool PlaceCard(CardInstance card)
        {
            if (card == null || card.IsDestroyed)
            {
                return false;
            }

            switch (card.Data.cardType)
            {
                case CardType.Tenant:
                    if (!CanPlaceTenant)
                    {
                        Debug.LogWarning($"[RoomSlot] Room {RoomIndex} tenant slots are full ({_tenants.Count}/{_maxTenantSlots}).");
                        return false;
                    }

                    _tenants.Add(card);
                    card.PlacedRoom = this;  // 建立卡牌到房间的引用
                    Debug.Log($"[RoomSlot] Room {RoomIndex} placed tenant: {card}");
                    return true;

                case CardType.Equipment:
                    if (!CanPlaceEquipment)
                    {
                        Debug.LogWarning($"[RoomSlot] Room {RoomIndex} equipment slots are full ({_equipments.Count}/{_maxEquipmentSlots}).");
                        return false;
                    }

                    _equipments.Add(card);
                    card.PlacedRoom = this;  // 建立卡牌到房间的引用
                    Debug.Log($"[RoomSlot] Room {RoomIndex} placed equipment: {card}");
                    return true;

                default:
                    Debug.LogWarning($"[RoomSlot] Event cards cannot be placed in a room: {card}");
                    return false;
            }
        }

        /// <summary>
        /// 从房间移除卡牌。同时清空卡牌的PlacedRoom引用。
        /// </summary>
        /// <param name="card">要移除的卡牌</param>
        /// <returns>是否成功移除（卡牌在房间内）</returns>
        public bool RemoveCard(CardInstance card)
        {
            if (card == null)
            {
                return false;
            }

            // 尝试从住户或装备列表移除
            bool removed = _tenants.Remove(card) || _equipments.Remove(card);
            if (removed)
            {
                card.PlacedRoom = null;  // 清空卡牌的房间引用
                Debug.Log($"[RoomSlot] Room {RoomIndex} removed card: {card}");
            }

            return removed;
        }

        /// <summary>
        /// 获取房间内所有的卡牌（住户+装备）。
        /// </summary>
        /// <returns>包含所有卡牌的新列表</returns>
        public List<CardInstance> GetAllCards()
        {
            var all = new List<CardInstance>(_tenants.Count + _equipments.Count);
            all.AddRange(_tenants);
            all.AddRange(_equipments);
            return all;
        }

        /// <summary>
        /// 获取房间内所有住户卡的只读列表。用于结算房租、应用住户效果等。
        /// </summary>
        /// <returns>住户列表的只读视图</returns>
        public IReadOnlyList<CardInstance> GetTenants() => _tenants;

        /// <summary>
        /// 获取房间内所有装备卡的只读列表。用于结算装备加成、应用装备效果等。
        /// </summary>
        /// <returns>装备列表的只读视图</returns>
        public IReadOnlyList<CardInstance> GetEquipments() => _equipments;

        /// <summary>
        /// 按索引获取房间内的住户卡。性能特点：O(1)。
        /// </summary>
        /// <param name="index">住户索引</param>
        /// <returns>住户卡实例，如果索引越界返回null</returns>
        public CardInstance GetTenantAt(int index)
        {
            return TryGetTenant(index, out var tenant) ? tenant : null;
        }

        /// <summary>
        /// 按索引获取房间内的装备卡。性能特点：O(1)。
        /// </summary>
        /// <param name="index">装备索引</param>
        /// <returns>装备卡实例，如果索引越界返回null</returns>
        public CardInstance GetEquipmentAt(int index)
        {
            return TryGetEquipment(index, out var equipment) ? equipment : null;
        }

        /// <summary>
        /// 尝试按索引获取住户卡。性能特点：O(1)。
        /// </summary>
        /// <param name="index">住户索引</param>
        /// <param name="tenant">如果成功获取，输出住户卡实例；否则为null</param>
        /// <returns>是否成功获取</returns>
        public bool TryGetTenant(int index, out CardInstance tenant)
        {
            if (index >= 0 && index < _tenants.Count)
            {
                tenant = _tenants[index];
                return true;
            }

            tenant = null;
            return false;
        }

        /// <summary>
        /// 尝试按索引获取装备卡。性能特点：O(1)。
        /// </summary>
        /// <param name="index">装备索引</param>
        /// <param name="equipment">如果成功获取，输出装备卡实例；否则为null</param>
        /// <returns>是否成功获取</returns>
        public bool TryGetEquipment(int index, out CardInstance equipment)
        {
            if (index >= 0 && index < _equipments.Count)
            {
                equipment = _equipments[index];
                return true;
            }

            equipment = null;
            return false;
        }

        /// <summary>
        /// 清理房间内所有被摧毁的卡牌。在回合结束或卡牌被销毁时调用。
        /// 由BoardManager.CleanupDestroyedCards()调用。
        /// </summary>
        public void CleanupDestroyedCards()
        {
            _tenants.RemoveAll(c => c == null || c.IsDestroyed);
            _equipments.RemoveAll(c => c == null || c.IsDestroyed);
        }

        /// <summary>
        /// 扩展房间的住户插槽容量。用于卡牌效果或升级机制。
        /// </summary>
        /// <param name="extraSlots">额外增加的插槽数</param>
        public void ExpandTenantSlots(int extraSlots)
        {
            _maxTenantSlots += extraSlots;
            Debug.Log($"[RoomSlot] Room {RoomIndex} tenant capacity expanded to {_maxTenantSlots}.");
        }
    }
}

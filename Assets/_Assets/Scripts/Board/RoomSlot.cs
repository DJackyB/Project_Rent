using System.Collections.Generic;
using UnityEngine;
using BaoZuPo.Card;

namespace BaoZuPo.Board
{
    /// <summary>
    /// 单个房间槽位
    /// 管理该房间内的租客和装备卡牌。
    /// </summary>
    public class RoomSlot : MonoBehaviour
    {
        [Header("房间配置")]
        [Tooltip("租客槽位上限（默认1，可通过装备扩展）")]
        [SerializeField] private int _maxTenantSlots = 1;

        [Tooltip("装备槽位上限（默认3）")]
        [SerializeField] private int _maxEquipmentSlots = 3;

        /// <summary>当前放置的租客卡牌</summary>
        private List<CardInstance> _tenants = new();

        /// <summary>当前放置的装备卡牌</summary>
        private List<CardInstance> _equipments = new();

        /// <summary>房间索引（由 BoardManager 分配）</summary>
        public int RoomIndex { get; set; }

        /// <summary>当前租客数量</summary>
        public int TenantCount => _tenants.Count;

        /// <summary>当前装备数量</summary>
        public int EquipmentCount => _equipments.Count;

        /// <summary>是否还能放租客</summary>
        public bool CanPlaceTenant => _tenants.Count < _maxTenantSlots;

        /// <summary>是否还能放装备</summary>
        public bool CanPlaceEquipment => _equipments.Count < _maxEquipmentSlots;

        /// <summary>
        /// 初始化房间（由 BoardManager 调用）
        /// </summary>
        public void Initialize(int index, int tenantSlots, int equipmentSlots)
        {
            RoomIndex = index;
            _maxTenantSlots = tenantSlots;
            _maxEquipmentSlots = equipmentSlots;
        }

        /// <summary>
        /// 放置卡牌到房间
        /// </summary>
        /// <returns>是否放置成功</returns>
        public bool PlaceCard(CardInstance card)
        {
            if (card == null || card.IsDestroyed) return false;

            switch (card.Data.cardType)
            {
                case CardType.Tenant:
                    if (!CanPlaceTenant)
                    {
                        Debug.LogWarning($"[RoomSlot] 房间{RoomIndex} 租客已满（{_tenants.Count}/{_maxTenantSlots}）");
                        return false;
                    }
                    _tenants.Add(card);
                    card.PlacedRoom = this;
                    Debug.Log($"[RoomSlot] 房间{RoomIndex} 放置租客: {card}");
                    return true;

                case CardType.Equipment:
                    if (!CanPlaceEquipment)
                    {
                        Debug.LogWarning($"[RoomSlot] 房间{RoomIndex} 装备已满（{_equipments.Count}/{_maxEquipmentSlots}）");
                        return false;
                    }
                    _equipments.Add(card);
                    card.PlacedRoom = this;
                    Debug.Log($"[RoomSlot] 房间{RoomIndex} 放置装备: {card}");
                    return true;

                default:
                    Debug.LogWarning($"[RoomSlot] 事件卡不能放入房间: {card}");
                    return false;
            }
        }

        /// <summary>
        /// 从房间移除卡牌
        /// </summary>
        public bool RemoveCard(CardInstance card)
        {
            bool removed = _tenants.Remove(card) || _equipments.Remove(card);
            if (removed)
            {
                card.PlacedRoom = null;
                Debug.Log($"[RoomSlot] 房间{RoomIndex} 移除卡牌: {card}");
            }
            return removed;
        }

        /// <summary>
        /// 获取房间内所有卡牌（租客 + 装备）
        /// </summary>
        public List<CardInstance> GetAllCards()
        {
            var all = new List<CardInstance>(_tenants.Count + _equipments.Count);
            all.AddRange(_tenants);
            all.AddRange(_equipments);
            return all;
        }

        /// <summary>获取所有租客</summary>
        public IReadOnlyList<CardInstance> GetTenants() => _tenants;

        /// <summary>获取所有装备</summary>
        public IReadOnlyList<CardInstance> GetEquipments() => _equipments;

        /// <summary>
        /// 移除所有已销毁的卡牌（清理用）
        /// </summary>
        public void CleanupDestroyedCards()
        {
            _tenants.RemoveAll(c => c.IsDestroyed);
            _equipments.RemoveAll(c => c.IsDestroyed);
        }

        /// <summary>
        /// 扩展租客槽位（通过装备卡效果调用）
        /// </summary>
        public void ExpandTenantSlots(int extraSlots)
        {
            _maxTenantSlots += extraSlots;
            Debug.Log($"[RoomSlot] 房间{RoomIndex} 租客槽位扩展至 {_maxTenantSlots}");
        }
    }
}

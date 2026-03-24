using System.Collections.Generic;
using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.Board
{
    /// <summary>
    /// A single room slot container for tenant and equipment cards.
    /// </summary>
    public class RoomSlot : MonoBehaviour
    {
        [Header("Room Config")]
        [Tooltip("Tenant slot capacity. Default: 1.")]
        [SerializeField] private int _maxTenantSlots = 1;

        [Tooltip("Equipment slot capacity. Default: 3.")]
        [SerializeField] private int _maxEquipmentSlots = 3;

        private readonly List<CardInstance> _tenants = new();
        private readonly List<CardInstance> _equipments = new();

        /// <summary>Room index assigned by BoardManager.</summary>
        public int RoomIndex { get; set; }

        /// <summary>Tenant slot capacity.</summary>
        public int TenantSlotCapacity => _maxTenantSlots;

        /// <summary>Equipment slot capacity.</summary>
        public int EquipmentSlotCapacity => _maxEquipmentSlots;

        /// <summary>Current tenant count.</summary>
        public int TenantCount => _tenants.Count;

        /// <summary>Current equipment count.</summary>
        public int EquipmentCount => _equipments.Count;

        /// <summary>Whether another tenant can be placed.</summary>
        public bool CanPlaceTenant => _tenants.Count < _maxTenantSlots;

        /// <summary>Whether another equipment can be placed.</summary>
        public bool CanPlaceEquipment => _equipments.Count < _maxEquipmentSlots;

        /// <summary>
        /// Initialize the room slot.
        /// </summary>
        public void Initialize(int index, int tenantSlots, int equipmentSlots)
        {
            RoomIndex = index;
            _maxTenantSlots = tenantSlots;
            _maxEquipmentSlots = equipmentSlots;
        }

        /// <summary>
        /// Place a card into this room.
        /// </summary>
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
                    card.PlacedRoom = this;
                    Debug.Log($"[RoomSlot] Room {RoomIndex} placed tenant: {card}");
                    return true;

                case CardType.Equipment:
                    if (!CanPlaceEquipment)
                    {
                        Debug.LogWarning($"[RoomSlot] Room {RoomIndex} equipment slots are full ({_equipments.Count}/{_maxEquipmentSlots}).");
                        return false;
                    }

                    _equipments.Add(card);
                    card.PlacedRoom = this;
                    Debug.Log($"[RoomSlot] Room {RoomIndex} placed equipment: {card}");
                    return true;

                default:
                    Debug.LogWarning($"[RoomSlot] Event cards cannot be placed in a room: {card}");
                    return false;
            }
        }

        /// <summary>
        /// Remove a card from the room.
        /// </summary>
        public bool RemoveCard(CardInstance card)
        {
            if (card == null)
            {
                return false;
            }

            bool removed = _tenants.Remove(card) || _equipments.Remove(card);
            if (removed)
            {
                card.PlacedRoom = null;
                Debug.Log($"[RoomSlot] Room {RoomIndex} removed card: {card}");
            }

            return removed;
        }

        /// <summary>
        /// Get all cards in this room.
        /// </summary>
        public List<CardInstance> GetAllCards()
        {
            var all = new List<CardInstance>(_tenants.Count + _equipments.Count);
            all.AddRange(_tenants);
            all.AddRange(_equipments);
            return all;
        }

        public IReadOnlyList<CardInstance> GetTenants() => _tenants;

        public IReadOnlyList<CardInstance> GetEquipments() => _equipments;

        public CardInstance GetTenantAt(int index)
        {
            return TryGetTenant(index, out var tenant) ? tenant : null;
        }

        public CardInstance GetEquipmentAt(int index)
        {
            return TryGetEquipment(index, out var equipment) ? equipment : null;
        }

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
        /// Remove destroyed cards from the room.
        /// </summary>
        public void CleanupDestroyedCards()
        {
            _tenants.RemoveAll(c => c == null || c.IsDestroyed);
            _equipments.RemoveAll(c => c == null || c.IsDestroyed);
        }

        /// <summary>
        /// Increase tenant capacity.
        /// </summary>
        public void ExpandTenantSlots(int extraSlots)
        {
            _maxTenantSlots += extraSlots;
            Debug.Log($"[RoomSlot] Room {RoomIndex} tenant capacity expanded to {_maxTenantSlots}.");
        }
    }
}

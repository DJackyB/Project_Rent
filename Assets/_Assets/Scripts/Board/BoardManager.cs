using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Core;
using UnityEngine;

namespace BaoZuPo.Board
{
    public class BoardManager : Singleton<BoardManager>
    {
        [Header("Debug Info")]
        [SerializeField] private List<RoomSlot> _rooms = new();
        [SerializeField] private List<CardInstance> _contracts = new();

        [Header("Scene References")]
        [SerializeField] private Transform _roomRoot;

        public int RoomCount => _rooms.Count;
        public int ContractCount => _contracts.Count;

        public void Initialize(int roomCount, int tenantSlots, int equipmentSlots)
        {
            if (_roomRoot == null)
            {
                Debug.LogError("[BoardManager] _roomRoot is not assigned in the Inspector. Create an empty GameObject named 'Rooms' in the scene and assign it.");
                return;
            }

            ClearAllRooms();
            _contracts.Clear();

            for (int i = 0; i < roomCount; i++)
            {
                AddRoom(tenantSlots, equipmentSlots);
            }

            Debug.Log($"[BoardManager] Initialized {roomCount} rooms.");
        }

        public RoomSlot AddRoom(int tenantSlots = 1, int equipmentSlots = 3)
        {
            var roomGO = new GameObject($"Room_{_rooms.Count}");
            roomGO.transform.SetParent(_roomRoot);

            var room = roomGO.AddComponent<RoomSlot>();
            room.Initialize(_rooms.Count, tenantSlots, equipmentSlots);
            _rooms.Add(room);

            Debug.Log($"[BoardManager] Added room: {room.RoomIndex}");
            return room;
        }

        public RoomSlot GetRoom(int index)
        {
            if (index < 0 || index >= _rooms.Count)
            {
                Debug.LogError($"[BoardManager] Room index out of range: {index}");
                return null;
            }

            return _rooms[index];
        }

        public IReadOnlyList<RoomSlot> GetAllRooms() => _rooms;

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

        public void AddContract(CardInstance contract)
        {
            if (contract == null || contract.IsDestroyed)
            {
                return;
            }

            _contracts.Add(contract);
        }

        public IReadOnlyList<CardInstance> GetAllContracts() => _contracts;

        public CardInstance GetContractAt(int index)
        {
            return TryGetContract(index, out var contract) ? contract : null;
        }

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

        public void CleanupDestroyedCards()
        {
            foreach (var room in _rooms)
            {
                room.CleanupDestroyedCards();
            }

            _contracts.RemoveAll(c => c == null || c.IsDestroyed);
        }

        private void ClearAllRooms()
        {
            foreach (var room in _rooms)
            {
                if (room != null)
                {
                    Destroy(room.gameObject);
                }
            }

            _rooms.Clear();
        }
    }
}

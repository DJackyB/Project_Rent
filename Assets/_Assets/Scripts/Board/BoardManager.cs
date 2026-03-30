using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Save;
using UnityEngine;

namespace BaoZuPo.Board
{
    /// <summary>
    /// Board manager that owns all rooms and contracts.
    /// </summary>
    public class BoardManager : Singleton<BoardManager>
    {
        [Header("Debug Info")]
        [SerializeField] private List<RoomSlot> _rooms = new();
        [SerializeField] private List<CardInstance> _contracts = new();

        [Header("Scene References")]
        [SerializeField] private Transform _roomRoot;

        /// <summary>Current room count.</summary>
        public int RoomCount => _rooms.Count;

        /// <summary>Current contract count.</summary>
        public int ContractCount => _contracts.Count;

        /// <summary>
        /// Initialize the board.
        /// </summary>
        public void Initialize(int roomCount, int tenantSlots, int equipmentSlots)
        {
            if (_roomRoot == null)
            {
                Debug.LogError("[BoardManager] _roomRoot 未在 Inspector 中赋值。请在场景中创建空 GameObject 'Rooms' 并拖入。");
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

        /// <summary>
        /// Add a new room dynamically.
        /// </summary>
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

        /// <summary>
        /// Get a room by index.
        /// </summary>
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
        /// Get all rooms.
        /// </summary>
        public IReadOnlyList<RoomSlot> GetAllRooms() => _rooms;

        /// <summary>
        /// Find the first room that can accept the given card type.
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
        /// Get all field cards, including room cards and contracts.
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
        /// Add a contract card.
        /// </summary>
        public void AddContract(CardInstance contract)
        {
            if (contract == null || contract.IsDestroyed)
            {
                return;
            }

            _contracts.Add(contract);
        }

        /// <summary>
        /// Get all contracts.
        /// </summary>
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

        /// <summary>
        /// Remove destroyed cards.
        /// </summary>
        public void CleanupDestroyedCards()
        {
            foreach (var room in _rooms)
            {
                room.CleanupDestroyedCards();
            }

            _contracts.RemoveAll(c => c == null || c.IsDestroyed);
        }

        public BoardSaveState CaptureState()
        {
            var state = new BoardSaveState();

            for (int i = 0; i < _rooms.Count; i++)
            {
                var room = _rooms[i];
                if (room == null)
                {
                    continue;
                }

                var roomState = new RoomSaveState
                {
                    roomIndex = room.RoomIndex,
                    tenantSlotCapacity = room.TenantSlotCapacity,
                    equipmentSlotCapacity = room.EquipmentSlotCapacity
                };

                CaptureCards(room.GetTenants(), roomState.tenants);
                CaptureCards(room.GetEquipments(), roomState.equipments);
                state.rooms.Add(roomState);
            }

            CaptureCards(_contracts, state.contracts);
            return state;
        }

        public void RestoreState(BoardSaveState state)
        {
            if (state == null)
            {
                throw new System.ArgumentNullException(nameof(state));
            }

            if (_roomRoot == null)
            {
                throw new System.InvalidOperationException("[BoardManager] Cannot restore board state without a room root.");
            }

            ClearAllRooms();
            _contracts.Clear();

            for (int i = 0; i < state.rooms.Count; i++)
            {
                var roomState = state.rooms[i];
                var room = AddRoom(roomState.tenantSlotCapacity, roomState.equipmentSlotCapacity);
                room.RoomIndex = roomState.roomIndex;

                RestoreCardsIntoRoom(room, roomState.tenants);
                RestoreCardsIntoRoom(room, roomState.equipments);
            }

            for (int i = 0; i < state.contracts.Count; i++)
            {
                if (!CardInstance.TryCreateFromRuntimeState(state.contracts[i], out var contract, out var error))
                {
                    throw new System.InvalidOperationException($"[BoardManager] Failed to restore contract #{i}. {error}");
                }

                AddContract(contract);
            }
        }

        private void ClearAllRooms()
        {
            foreach (var room in _rooms)
            {
                if (room != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(room.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(room.gameObject);
                    }
                }
            }

            _rooms.Clear();
        }

        private static void CaptureCards(IReadOnlyList<CardInstance> source, List<CardRuntimeState> target)
        {
            if (source == null || target == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                var card = source[i];
                if (card == null || card.IsDestroyed)
                {
                    continue;
                }

                target.Add(card.CaptureRuntimeState());
            }
        }

        private static void RestoreCardsIntoRoom(RoomSlot room, IReadOnlyList<CardRuntimeState> states)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (!CardInstance.TryCreateFromRuntimeState(states[i], out var card, out var error))
                {
                    throw new System.InvalidOperationException($"[BoardManager] Failed to restore room card #{i}. {error}");
                }

                if (!room.PlaceCard(card))
                {
                    throw new System.InvalidOperationException($"[BoardManager] Failed to place restored card '{card}'.");
                }
            }
        }
    }
}

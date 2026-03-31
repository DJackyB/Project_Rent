using BaoZuPo.Board;
using BaoZuPo.Deck;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaoZuPo.Core
{
    public class GameDebugController : MonoBehaviour
    {
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame && !TurnManager.Instance.ActionPhaseEnded)
            {
                TurnManager.Instance.EndActionPhase();
            }

            if (keyboard.dKey.wasPressedThisFrame)
            {
                PrintGameState();
            }

            HandleCardInput(keyboard);
        }

        private void HandleCardInput(Keyboard keyboard)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) PlayCardAtIndex(0);
            if (keyboard.digit2Key.wasPressedThisFrame) PlayCardAtIndex(1);
            if (keyboard.digit3Key.wasPressedThisFrame) PlayCardAtIndex(2);
            if (keyboard.digit4Key.wasPressedThisFrame) PlayCardAtIndex(3);
            if (keyboard.digit5Key.wasPressedThisFrame) PlayCardAtIndex(4);
            if (keyboard.digit6Key.wasPressedThisFrame) PlayCardAtIndex(5);
            if (keyboard.digit7Key.wasPressedThisFrame) PlayCardAtIndex(6);
            if (keyboard.digit8Key.wasPressedThisFrame) PlayCardAtIndex(7);
            if (keyboard.digit9Key.wasPressedThisFrame) PlayCardAtIndex(8);
        }

        private void PlayCardAtIndex(int index)
        {
            var hand = DeckManager.Instance.Hand;
            if (index >= hand.Count)
            {
                Debug.LogWarning($"[Debug] Hand index {index + 1} is out of range. Current hand count: {hand.Count}.");
                return;
            }

            var card = hand[index];
            var targetRoom = BoardManager.Instance.FindAvailableRoom(card.Data);

            if (TurnManager.Instance.GetRequiredTargetKind(card) == CardPlayTargetKind.Room && targetRoom == null)
            {
                Debug.LogWarning($"[Debug] No valid room target is available for card: {card.Data.cardName}");
                return;
            }

            bool success = TurnManager.Instance.PlayCard(card, targetRoom);
            if (success)
            {
                Debug.Log($"[Debug] Played card from hotkey [{index + 1}]: {card.Data.cardName}");
            }
        }

        private void PrintGameState()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========== Current Game State ==========");
            sb.AppendLine($"Turn: {TurnManager.Instance.CurrentTurn}");
            sb.AppendLine($"Money: {MoneyManager.Instance.CurrentMoney}");
            sb.AppendLine($"Hand Count: {DeckManager.Instance.HandCount}");

            foreach (var card in DeckManager.Instance.Hand)
            {
                sb.AppendLine($"  - [Hand] {card}");
            }

            var rooms = BoardManager.Instance.GetAllRooms();
            foreach (var room in rooms)
            {
                sb.AppendLine($"Room_{room.RoomIndex} (Tenants:{room.TenantCount}, Equipment:{room.EquipmentCount})");
                foreach (var card in room.GetAllCards())
                {
                    sb.AppendLine($"  - [Field] {card}");
                }
            }

            sb.AppendLine("========================================");
            Debug.Log(sb.ToString());
        }
    }
}

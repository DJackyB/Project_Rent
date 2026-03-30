using BaoZuPo.Board;
using BaoZuPo.Deck;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using BaoZuPo.Localization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaoZuPo.Core
{
    /// <summary>
    /// 游戏调试控制器（支持新版 Input System）
    /// 如果仍然报错，请确认 Unity Console 中没有其他编译错误阻止脚本更新。
    /// </summary>
    public class GameDebugController : MonoBehaviour
    {
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                if (!TurnManager.Instance.ActionPhaseEnded)
                {
                    TurnManager.Instance.EndActionPhase();
                }
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
                Debug.LogWarning($"[Debug] 卡牌索引 {index + 1} 超出范围（当前手牌 {hand.Count} 张）");
                return;
            }

            var card = hand[index];
            var targetRoom = BoardManager.Instance.FindAvailableRoom(card.Data.cardType);

            if (card.Data.cardType != Card.CardType.Event && targetRoom == null)
            {
                Debug.LogWarning($"[Debug] 没有可用的房间来放置卡牌: {CardTextResolver.ResolveName(card)}");
                return;
            }

            bool success = TurnManager.Instance.PlayCard(card, targetRoom);
            if (success)
            {
                Debug.Log($"[Debug] 成功通过按键 [{index + 1}] 打出: {CardTextResolver.ResolveName(card)}");
            }
        }

        private void PrintGameState()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========== 当前游戏状态 ==========");
            sb.AppendLine($"回合数: {TurnManager.Instance.CurrentTurn}");
            sb.AppendLine($"当前金钱: {MoneyManager.Instance.CurrentMoney}");
            sb.AppendLine($"手牌数量: {DeckManager.Instance.HandCount}");

            foreach (var card in DeckManager.Instance.Hand)
            {
                sb.AppendLine($"  - [手牌] {card}");
            }

            var rooms = BoardManager.Instance.GetAllRooms();
            foreach (var room in rooms)
            {
                sb.AppendLine($"房间_{room.RoomIndex} (租客:{room.TenantCount}, 设备:{room.EquipmentCount})");
                foreach (var card in room.GetAllCards())
                {
                    sb.AppendLine($"  - [场上] {card}");
                }
            }

            sb.AppendLine("================================");
            Debug.Log(sb.ToString());
        }
    }
}

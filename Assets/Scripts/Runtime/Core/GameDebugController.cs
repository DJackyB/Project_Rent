using BaoZuPo.Board;
using BaoZuPo.Deck;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using Martian.RandomEvent;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace BaoZuPo.Core
{
    /// <summary>
    /// 游戏调试控制器。
    /// 在开发阶段使用键盘快捷键快速测试游戏流程。
    ///
    /// 快捷键：
    /// - Space：结束行动阶段（进入结算）
    /// - D：打印当前游戏状态（回合数、金钱、手牌、棋盘）
    /// - 1-9：快速从手牌 1-9 位置打出卡牌（自动寻找合法目标）
    ///
    /// 仅在开发阶段使用，发布版本应移除或禁用此脚本。
    /// </summary>
    public class GameDebugController : MonoBehaviour
    {
        [SerializeField] private string _debugEventId = "event_tenant_complaint";

        [ContextMenu("触发随机事件")]
        public void TriggerDebugRandomEvent()
        {
            RandomEventManager.Instance?.TriggerEvent(_debugEventId);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            // Space：快速结束行动阶段
            if (keyboard.spaceKey.wasPressedThisFrame && !TurnManager.Instance.ActionPhaseEnded)
            {
                TurnManager.Instance.EndActionPhase();
            }

            // D：打印游戏状态
            if (keyboard.dKey.wasPressedThisFrame)
            {
                PrintGameState();
            }

            // 1-9：快速打出卡牌
            HandleCardInput(keyboard);
        }

        /// <summary>
        /// 处理 1-9 键打出卡牌（从手牌 0-8 位置）。
        /// </summary>
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

        /// <summary>
        /// 从手牌指定位置打出卡牌。
        /// 自动寻找合法目标（如需要指定房间，则自动选择可用的房间）。
        /// </summary>
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

        /// <summary>
        /// 打印当前游戏状态：回合数、金钱、手牌、棋盘布局。
        /// </summary>
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

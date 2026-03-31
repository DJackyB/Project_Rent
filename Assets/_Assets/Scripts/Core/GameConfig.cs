using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "BaoZuPo/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Economy")]
        [Tooltip("Starting money.")]
        public int startingMoney = 1000;

        [Tooltip("Loan payment amount per cycle.")]
        public int loanAmount = 500;

        [Tooltip("Number of turns between loan payments.")]
        public int loanInterval = 5;

        [Tooltip("Growth factor applied to later loan payments.")]
        public float loanGrowthFactor = 2f;

        [Header("Draw")]
        [Tooltip("Cards drawn on the first turn.")]
        public int firstTurnDrawCount = 5;

        [Tooltip("Cards drawn on a normal turn.")]
        public int normalTurnDrawCount = 3;

        [Tooltip("Maximum hand size.")]
        public int maxHandSize = 7;

        [Tooltip("Card library used for the first turn draw.")]
        public CardLibrary firstTurnDrawLibrary;

        [Tooltip("Card library used for the normal turn draw.")]
        public CardLibrary normalTurnDrawLibrary;

        [Tooltip("Card library used for reward selection.")]
        public CardLibrary rewardLibrary;

        [Header("Rooms")]
        [Tooltip("Initial room count.")]
        public int initialRoomCount = 3;

        [Tooltip("Default tenant slot count per room.")]
        public int defaultTenantSlots = 1;

        [Tooltip("Default equipment slot count per room.")]
        public int defaultEquipmentSlots = 3;

        [Header("Feedback")]
        [Tooltip("Enable the shared floating feedback module.")]
        public bool enableFeedback = true;

        [Tooltip("Enable money delta floating feedback.")]
        public bool enableMoneyFeedback = true;

        [Tooltip("Enable debug logs for the feedback module.")]
        public bool enableFeedbackLogs = true;
    }
}

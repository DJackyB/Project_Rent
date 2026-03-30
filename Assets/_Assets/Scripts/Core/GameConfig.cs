using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "BaoZuPo/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("经济")]
        [Tooltip("初始资金")]
        public int startingMoney = 1000;

        [Tooltip("每次还贷金额")]
        public int loanAmount = 500;

        [Tooltip("每隔多少回合还贷一次")]
        public int loanInterval = 5;

        [Tooltip("后续每次还贷金额的增长系数")]
        public float loanGrowthFactor = 2f;

        [Header("抽牌")]
        [Tooltip("首回合抽牌数")]
        public int firstTurnDrawCount = 5;

        [Tooltip("普通回合抽牌数")]
        public int normalTurnDrawCount = 3;

        [Tooltip("最大手牌上限")]
        public int maxHandSize = 7;

        [Tooltip("首回合抽牌所使用的牌库")]
        public CardLibrary firstTurnDrawLibrary;

        [Tooltip("普通回合抽牌所使用的牌库")]
        public CardLibrary normalTurnDrawLibrary;

        [Tooltip("奖励三选一所使用的牌库")]
        public CardLibrary rewardLibrary;

        [Header("房间")]
        [Tooltip("初始房间数量")]
        public int initialRoomCount = 3;

        [Tooltip("每个房间默认租客槽位数")]
        public int defaultTenantSlots = 1;

        [Tooltip("每个房间默认装备槽位数")]
        public int defaultEquipmentSlots = 3;

        [Header("跳字反馈")]
        [Tooltip("是否启用通用跳字反馈模块")]
        public bool enableFeedback = true;

        [Tooltip("是否启用资金变化的跳字反馈")]
        public bool enableMoneyFeedback = true;

        [Tooltip("是否输出跳字反馈模块调试日志")]
        public bool enableFeedbackLogs = true;
    }
}

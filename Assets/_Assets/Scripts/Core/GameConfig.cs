using UnityEngine;

namespace BaoZuPo.Core
{
    /// <summary>
    /// 全局游戏配置
    /// 存放所有可调参数，Inspector 中可直接修改。
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "包租婆/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("经济")]
        [Tooltip("初始金钱")]
        public int startingMoney = 1000;

        [Tooltip("每次还贷扣款金额")]
        public int loanAmount = 500;

        [Tooltip("还贷间隔（每隔多少回合还一次贷）")]
        public int loanInterval = 5;

        [Header("抽卡")]
        [Tooltip("第一回合抽卡数量")]
        public int firstTurnDrawCount = 5;

        [Tooltip("后续回合抽卡数量")]
        public int normalTurnDrawCount = 3;

        [Tooltip("手牌上限，达到上限时本次抽卡跳过")]
        public int maxHandSize = 7;

        [Header("房间")]
        [Tooltip("初始房间数量")]
        public int initialRoomCount = 3;

        [Tooltip("每个房间默认租客槽位数")]
        public int defaultTenantSlots = 1;

        [Tooltip("每个房间默认装备槽位数")]
        public int defaultEquipmentSlots = 3;
    }
}

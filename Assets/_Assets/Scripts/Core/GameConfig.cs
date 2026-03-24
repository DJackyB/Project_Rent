using UnityEngine;

namespace BaoZuPo.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "BaoZuPo/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("\u7ecf\u6d4e")]
        [Tooltip("\u521d\u59cb\u8d44\u91d1")]
        public int startingMoney = 1000;

        [Tooltip("\u6bcf\u6b21\u8fd8\u8d37\u91d1\u989d")]
        public int loanAmount = 500;

        [Tooltip("\u6bcf\u9694\u591a\u5c11\u56de\u5408\u8fd8\u8d37\u4e00\u6b21")]
        public int loanInterval = 5;

        [Tooltip("\u540e\u7eed\u6bcf\u6b21\u8fd8\u8d37\u91d1\u989d\u7684\u589e\u957f\u7cfb\u6570")]
        public float loanGrowthFactor = 2f;

        [Header("\u62bd\u724c")]
        [Tooltip("\u9996\u56de\u5408\u62bd\u724c\u6570")]
        public int firstTurnDrawCount = 5;

        [Tooltip("\u666e\u901a\u56de\u5408\u62bd\u724c\u6570")]
        public int normalTurnDrawCount = 3;

        [Tooltip("\u6700\u5927\u624b\u724c\u4e0a\u9650")]
        public int maxHandSize = 7;

        [Header("\u623f\u95f4")]
        [Tooltip("\u521d\u59cb\u623f\u95f4\u6570\u91cf")]
        public int initialRoomCount = 3;

        [Tooltip("\u6bcf\u4e2a\u623f\u95f4\u9ed8\u8ba4\u79df\u5ba2\u69fd\u4f4d\u6570")]
        public int defaultTenantSlots = 1;

        [Tooltip("\u6bcf\u4e2a\u623f\u95f4\u9ed8\u8ba4\u88c5\u5907\u69fd\u4f4d\u6570")]
        public int defaultEquipmentSlots = 3;
    }
}

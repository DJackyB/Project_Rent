using System;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌库条目：记录某张牌在某个牌库中的份数。
    ///
    /// 用途：
    /// - 初始化抽卡池时，按 quantity 决定放几份该牌进牌堆
    /// - 随机抽取（奖励池等）时忽略 quantity，仅使用 card 列表
    ///
    /// 数据来源：Excel CardLibrary sheet（libraryId | cardId | quantity）
    /// </summary>
    [Serializable]
    public class CardLibraryEntry
    {
        public CardData card;

        [Min(1)]
        public int quantity = 1;
    }
}

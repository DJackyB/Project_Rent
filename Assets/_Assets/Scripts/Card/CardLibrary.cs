using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌库配置资源（ScriptableObject）。
    ///
    /// 定义一个特定的卡牌集合，用于抽牌、奖励等机制。
    /// 每个库有唯一的 libraryId，可在效果字符串中按名称引用。
    ///
    /// 用途示例：
    /// - "0" 库（FirstTurnPool）：第一回合抽卡池
    /// - "1" 库（NormalTurnPool）：主抽卡池，游戏开始时据此初始化牌堆
    /// - "2" 库（RewardPool）：奖励阶段卡池
    ///
    /// 配置方式：
    /// 在 Excel 的 CardLibrary sheet 中维护（libraryId | cardId | quantity），
    /// 手动运行 CardDataImporter 同步卡库。
    /// </summary>
    [CreateAssetMenu(fileName = "NewCardLibrary", menuName = "BaoZuPo/Card Library")]
    public class CardLibrary : ScriptableObject
    {
        /// <summary>
        /// 库标识符。必须唯一，用于效果字符串和代码查询。
        /// </summary>
        [Tooltip("Stable id used by effect strings such as DrawCard;2;EventPool.")]
        public string libraryId;

        /// <summary>
        /// 显示名称。用于 Inspector 和日志中的可读表现。
        /// 如果为空，则使用资源文件名（name）。
        /// </summary>
        [Tooltip("Optional display name used in inspector and logs.")]
        public string displayName;

        /// <summary>
        /// 卡牌条目列表。每条记录包含一张卡牌及其在初始牌堆中的份数。
        /// 数据由 CardDataImporter 从 Excel 的 CardLibrary sheet 同步生成，勿手动修改。
        /// </summary>
        [Tooltip("Card entries with quantity. Synced from Excel CardLibrary sheet.")]
        public List<CardLibraryEntry> entries = new();

        /// <summary>返回该库的显示名称。优先使用 displayName，否则使用资源名。</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}

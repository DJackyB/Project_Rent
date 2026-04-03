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
    /// - "Main" 库：基础卡池
    /// - "EventPool" 库：事件卡专用池
    /// - "Reward" 库：奖励阶段卡池
    ///
    /// 配置方式：
    /// 1. 在 Inspector 中创建新 CardLibrary.asset
    /// 2. 设置 libraryId（如 "EventPool"）
    /// 3. 拖拽卡牌到 cards 列表
    /// 4. 重复卡牌表示权重（同一卡重复 3 次表示 3x 权重）
    /// </summary>
    [CreateAssetMenu(fileName = "NewCardLibrary", menuName = "BaoZuPo/Card Library")]
    public class CardLibrary : ScriptableObject
    {
        /// <summary>
        /// 库标识符。必须唯一，用于效果字符串和代码查询。
        /// 格式建议：英文、下划线、无空格（如 "MainPool"、"Event_Deck"）。
        /// 用于 DrawCard;N;libraryId 格式的效果字符串。
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
        /// 卡牌列表。明确指定库中的卡牌及其权重。
        /// 相同卡牌出现多次表示权重加倍（用于调整稀有度）。
        /// 示例：[A, B, B, C] 表示 A:25%, B:50%, C:25%。
        /// </summary>
        [Tooltip("Explicit card list. Repeated entries count as extra copies/weight.")]
        public List<CardData> cards = new();

        /// <summary>返回该库的显示名称。优先使用 displayName，否则使用资源名。</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}

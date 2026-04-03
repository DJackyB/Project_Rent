using System.Collections.Generic;
using UnityEngine;

namespace Martian.RandomEvent
{
    /// <summary>
    /// 随机事件库。将多个事件分组，支持加权随机选取。
    ///
    /// 用途：
    /// - 按场景或阶段分类事件（如"第一幕事件"、"战役随机事件"）
    /// - RandomEventManager.TriggerRandomFromLibrary() 从库中随机选取事件
    ///
    /// 权重机制：
    /// - 列表中重复的事件表示权重
    /// - 例如：[EventA, EventA, EventB] 则 EventA 被选取的概率是 EventB 的 2 倍
    /// - RandomEventManager 直接随机索引：Random.Range(0, events.Count)
    ///
    /// 创建方法：
    /// 1. 右键 Project > Create > Martian > Random Event Library
    /// 2. 设置 libraryId（如 "act1_events"）
    /// 3. 将 RandomEventData 资产拖入 events 列表
    /// 4. 根据需要重复某些事件以调整权重
    /// 5. 保存到 Resources/RandomEventLibraries/ 目录
    /// </summary>
    [CreateAssetMenu(fileName = "NewRandomEventLibrary", menuName = "Martian/Random Event Library")]
    public class RandomEventLibrary : ScriptableObject
    {
        /// <summary>
        /// 库的唯一标识。
        /// 用法：RandomEventManager.TriggerRandomFromLibrary("libraryId")
        /// </summary>
        [Tooltip("Stable identifier used for runtime lookup.")]
        public string libraryId;

        /// <summary>
        /// 可选的显示名称。用于 Inspector 和日志。
        /// 若为空，使用资产文件名。
        /// </summary>
        [Tooltip("Optional display name for inspector and logs.")]
        public string displayName;

        /// <summary>
        /// 事件列表。支持重复以表示权重。
        /// 例如：[EventA, EventA, EventB] 表示 EventA 权重为 2。
        /// RandomEventManager 会随机选取一个（UnityEngine.Random.Range(0, events.Count)）。
        /// </summary>
        [Tooltip("Event entries. Repeated entries count as extra weight.")]
        public List<RandomEventData> events = new();

        /// <summary>
        /// 显示名称属性。若 displayName 未设置，返回资产文件名。
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}

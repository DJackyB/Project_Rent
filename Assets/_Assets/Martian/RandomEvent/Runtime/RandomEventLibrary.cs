using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martian.RandomEvent
{
    /// <summary>
    /// 随机事件库条目。一个事件及其权重。
    /// weight 越大，被随机选中的概率越高（绝对权重，非相对倍数）。
    /// </summary>
    [Serializable]
    public class RandomEventEntry
    {
        [Tooltip("The event definition.")]
        public RandomEventData data;

        [Tooltip("Relative weight. Higher = more likely to be picked.")]
        [Min(1)]
        public int weight = 1;
    }

    /// <summary>
    /// 随机事件库。将多个事件分组，支持加权随机选取。
    ///
    /// 用途：
    /// - 按场景或阶段分类事件（如"第一幕事件"、"战役随机事件"）
    /// - RandomEventManager.TriggerRandomFromLibrary() 从库中随机选取事件
    ///
    /// 权重机制：
    /// - 每条 entry 有独立的 weight 字段（整数，≥1）
    /// - 例如：EventA(weight=2) + EventB(weight=1)，EventA 被选中概率约 66%
    ///
    /// 创建方法：
    /// 1. 右键 Project > Create > Martian > Random Event Library
    /// 2. 设置 libraryId（如 "act1_events"）
    /// 3. 将 RandomEventData 资产拖入 entries 列表，设置各自 weight
    /// 4. 保存到 Resources/RandomEventLibraries/ 目录
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
        /// 事件条目列表。每条含事件引用和权重。
        /// </summary>
        [Tooltip("Event entries with individual weights.")]
        public List<RandomEventEntry> entries = new();

        /// <summary>
        /// 显示名称属性。若 displayName 未设置，返回资产文件名。
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}

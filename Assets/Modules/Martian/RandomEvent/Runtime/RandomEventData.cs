using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martian.RandomEvent
{
    /// <summary>
    /// 随机事件数据定义。以 ScriptableObject 资产形式存储事件的所有数据。
    ///
    /// 用途：
    /// - 定义单个随机事件（如"获得奖励"、"怪物入侵"等）
    /// - 在 Project 中创建，由 RandomEventManager 在运行时加载和触发
    ///
    /// 字段说明：
    /// - eventId：唯一标识，用于 RandomEventManager.TriggerEvent(eventId)
    /// - tags：业务标签（由项目自定义含义），RandomEventManager 不解释
    /// - titleKey、descriptionKey：支持 {0} {1} 占位符，在触发时替换
    /// - artwork：事件图标或背景
    /// - options：玩家可选择的选项列表（至少需要一个）
    ///
    /// 创建方法：
    /// 1. 右键 Project > Create > Martian > Random Event Data
    /// 2. 设置 eventId（如 "event_treasure"）
    /// 3. 填入标题、描述、图标
    /// 4. 添加至少一个选项
    /// 5. 保存到 Resources/RandomEvents/ 目录（供 RandomEventDatabase.LoadAll() 加载）
    /// </summary>
    [CreateAssetMenu(fileName = "NewRandomEvent", menuName = "Martian/Random Event Data")]
    public class RandomEventData : ScriptableObject
    {
        /// <summary>
        /// 事件的唯一标识。
        /// 用法：RandomEventManager.TriggerEvent("myEventId")
        /// </summary>
        [Tooltip("Unique event identifier.")]
        public string eventId;

        /// <summary>
        /// 可选的事件标签。由项目自定义含义（如"困难"、"快速"等）。
        /// RandomEventManager 不依赖或解释这些标签，仅作为数据存储。
        /// </summary>
        [Tooltip("Optional tags for business-side filtering. The module does not interpret them.")]
        public string[] tags = Array.Empty<string>();

        /// <summary>
        /// 事件标题文本。可包含 {0}, {1} 等占位符。
        /// RandomEventManager 会在触发时用 RandomEventRequest.DescriptionArgs 填充。
        /// 示例："获得 {0} 个金币"
        /// </summary>
        [Tooltip("Event title. Supports {0} placeholders filled at trigger time.")]
        public string titleKey;

        /// <summary>
        /// 事件描述文本。可包含 {0}, {1} 等占位符。
        /// 示例："一个 {0} 突然出现。如何应对？"
        /// </summary>
        [Tooltip("Event description. Supports {0} {1} placeholders filled at trigger time.")]
        [TextArea(3, 6)]
        public string descriptionKey;

        /// <summary>
        /// 事件的视觉表现：图标或背景图片。
        /// 由 UI 演示者显示。
        /// </summary>
        [Tooltip("Event artwork / icon.")]
        public Sprite artwork;

        /// <summary>
        /// 玩家可选择的选项列表（至少需要一个）。
        /// RandomEventDatabase.ValidateEvent() 会检查此列表非空。
        /// </summary>
        [Tooltip("Player-selectable options.")]
        public List<RandomEventOption> options = new();
    }

    /// <summary>
    /// 随机事件选项。代表玩家在事件中的一个可选行为。
    ///
    /// 字段说明：
    /// - optionId：此选项的唯一标识（在事件内唯一）
    /// - displayTextKey：按钮上显示的文本
    /// - tooltipKey：鼠标悬停时的提示（可选）
    /// </summary>
    [Serializable]
    public class RandomEventOption
    {
        /// <summary>
        /// 选项的唯一标识（在事件内唯一）。
        /// 用法：在 RandomEventOptionSelected 事件中返回给回调处理。
        /// </summary>
        [Tooltip("Unique option identifier within this event.")]
        public string optionId;

        /// <summary>
        /// 选项按钮上显示的文本。可包含 {0} 占位符。
        /// 示例："接受(+{0}金币)"
        /// </summary>
        [Tooltip("Option button text. Supports {0} placeholders.")]
        public string displayTextKey;

        /// <summary>
        /// 可选的提示文本。
        /// 当玩家悬停在选项上时显示额外信息（如后果）。
        /// </summary>
        [Tooltip("Optional tooltip explaining consequences.")]
        public string tooltipKey;

        /// <summary>
        /// 选项效果字符串，格式与卡牌效果一致（支持 | 多效果）。
        /// 示例："AddMoney;-200" 或 "ReduceMoney;50|DrawCard;1"
        /// </summary>
        [Tooltip("Effect string executed when this option is selected. Same format as card effects.")]
        public string effect;

        /// <summary>
        /// 选项选完后立即触发的后续事件 ID（事件链）。留空=无后续。
        /// </summary>
        [Tooltip("Event ID to trigger immediately after this option is selected. Leave empty for no chain.")]
        public string nextEventId;
    }
}

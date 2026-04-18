using System;

namespace Martian.Save
{
    /// <summary>
    /// 存档片段接口。一个 ISaveSection 代表一个独立的数据领域（如玩家经济、卡牌库等）。
    ///
    /// 三层架构中的第一层（数据模型层）：
    /// - SaveService 不关心具体数据如何组织
    /// - 各个数据领域通过实现此接口来参与存档系统
    ///
    /// 实现必须提供：
    /// - CaptureState()：序列化当前状态
    /// - ApplyState()：反序列化并应用状态
    /// - TryValidate()：验证状态合法性
    /// - Key：唯一标识该片段
    /// </summary>
    public interface ISaveSection
    {
        /// <summary>
        /// 此片段的唯一键。用于在存档文件中标识。
        /// 示例：economy、cardlibrary、player_stats
        /// </summary>
        string Key { get; }

        /// <summary>
        /// 是否为必需片段。若为 true 且存档中缺失该片段，加载会失败。
        /// 可选片段缺失时被忽略。
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// 状态对象的 C# 类型。SaveService 用此进行反序列化。
        /// 示例：typeof(EconomyState)
        /// </summary>
        Type StateType { get; }

        /// <summary>
        /// 捕获当前状态。返回一个可序列化的数据对象。
        /// 调用方：SaveService（保存时）
        /// 返回值：应为简单数据对象（不含引用），可被 ISaveSerializer.Serialize() 处理
        /// </summary>
        /// <returns>状态对象，或 null（会被 SaveService 拒绝）</returns>
        object CaptureState();

        /// <summary>
        /// 验证反序列化后的状态。
        /// 用于检查数据一致性、范围合法性等。
        /// </summary>
        /// <param name="state">要验证的状态对象</param>
        /// <param name="error">若验证失败，填入错误描述；否则为 null</param>
        /// <returns>验证通过返回 true</returns>
        bool TryValidate(object state, out string error);

        /// <summary>
        /// 应用状态。反序列化后，SaveService 会调用此方法将状态应用到游戏世界。
        /// 通常更新单例或 Manager 的内部状态。
        /// </summary>
        /// <param name="state">已验证的状态对象</param>
        void ApplyState(object state);
    }
}

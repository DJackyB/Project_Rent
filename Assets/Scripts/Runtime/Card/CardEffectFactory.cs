using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌效果工厂。
    ///
    /// 负责将 CardData 中的效果字符串解析为可执行的 ICardEffect 对象。
    /// 通过反射和工厂委托方式支持可扩展的效果注册机制。
    ///
    /// 效果字符串格式：
    /// 单个效果：  "AddMoney;100"
    /// 多个效果：  "AddMoney;100 | DrawCard;2 | ExpandSlot;1"（用 | 分隔）
    ///
    /// 工作流：
    /// 1. 初始化时，CardEffectRegistration.EnsureRegistered() 注册所有内置效果
    /// 2. 每个效果由一个工厂委托实现，将参数字符串转换为具体效果对象
    /// 3. Create() 或 TryValidate() 时，工厂解析字符串并返回 ICardEffect
    /// 4. 执行时，GameContext 提供游戏状态访问
    ///
    /// 错误处理：
    /// - 未注册的效果 ID 会记录警告，返回 null
    /// - 参数数量不足会抛出 ArgumentException
    /// - 解析失败的效果会记录错误，返回 null
    /// </summary>
    public static class CardEffectFactory
    {
        /// <summary>
        /// 复合效果。包装多个效果，按顺序执行。
        ///
        /// 用于处理包含多个 | 分隔符的效果字符串。
        /// 如果某个效果为 null，则跳过执行（静默处理）。
        /// </summary>
        private sealed class CompositeCardEffect : ICardEffect
        {
            private readonly List<ICardEffect> _effects;

            public CompositeCardEffect(List<ICardEffect> effects)
            {
                _effects = effects;
            }

            /// <summary>依次执行包含的所有效果。</summary>
            public void Execute(CardInstance source, GameContext context)
            {
                foreach (var effect in _effects)
                {
                    effect?.Execute(source, context);
                }
            }
        }

        /// <summary>
        /// 效果注册表。
        /// 键：效果 ID（如 "AddMoney"、"DrawCard"）
        /// 值：工厂委托，接收参数字符串数组，返回 ICardEffect 实例
        /// </summary>
        private static readonly Dictionary<string, Func<string[], ICardEffect>> _registry = new();

        /// <summary>
        /// 注册一个效果工厂（无参数数量检查）。
        ///
        /// 参数：
        /// - effectId：效果标识符（如 "AddMoney"）
        /// - factory：工厂委托，接收参数数组，返回 ICardEffect 实例
        ///
        /// 示例：
        /// Register("AddRoom", _ => new AddRoomEffect());
        /// </summary>
        public static void Register(string effectId, Func<string[], ICardEffect> factory)
        {
            _registry[effectId] = factory;
        }

        /// <summary>
        /// 注册一个效果工厂（含参数数量检查）。
        ///
        /// 参数：
        /// - effectId：效果标识符
        /// - expectedArgCount：期望的参数个数
        /// - factory：工厂委托
        ///
        /// 如果实际参数数量少于期望值，会抛出 ArgumentException。
        ///
        /// 示例：
        /// Register("AddMoney", 1, args => new AddMoneyEffect(int.Parse(args[0])));
        /// </summary>
        public static void Register(string effectId, int expectedArgCount, Func<string[], ICardEffect> factory)
        {
            _registry[effectId] = args =>
            {
                if (args.Length < expectedArgCount)
                {
                    throw new ArgumentException($"expected {expectedArgCount} arg(s) but got {args.Length}");
                }
                return factory(args);
            };
        }

        /// <summary>
        /// 从效果字符串创建 ICardEffect 对象。
        ///
        /// 支持单个和复合效果：
        /// - 单个效果：返回该效果对象（或 null，如失败）
        /// - 空字符串：返回 null
        /// - 多个效果（用 | 分隔）：
        ///   - 如果所有效果都失败，返回 null
        ///   - 如果只有一个效果成功，返回该效果
        ///   - 否则返回 CompositeCardEffect 包装器
        ///
        /// 参数：
        /// - effectString：效果字符串。示例："AddMoney;100" 或 "AddMoney;100|DrawCard;2"
        ///
        /// 返回：ICardEffect 实例，或 null（如果字符串为空或所有效果解析失败）
        ///
        /// 注意：解析失败时会记录警告/错误日志，但不会抛出异常。
        /// </summary>
        public static ICardEffect Create(string effectString)
        {
            if (string.IsNullOrWhiteSpace(effectString))
            {
                return null;
            }

            if (effectString.Contains("|"))
            {
                var segments = effectString.Split('|');
                var effects = new List<ICardEffect>();
                foreach (var rawSegment in segments)
                {
                    var segment = rawSegment.Trim();
                    if (string.IsNullOrEmpty(segment))
                    {
                        continue;
                    }

                    var effect = CreateSingle(segment);
                    if (effect != null)
                    {
                        effects.Add(effect);
                    }
                }

                if (effects.Count == 0)
                {
                    return null;
                }

                if (effects.Count == 1)
                {
                    return effects[0];
                }

                return new CompositeCardEffect(effects);
            }

            return CreateSingle(effectString.Trim());
        }

        /// <summary>
        /// 验证效果字符串的合法性，不实际创建效果对象。
        ///
        /// 用于 Editor 验证、数据导入或启动检查。
        /// 支持单个和复合效果的验证。
        ///
        /// 参数：
        /// - effectString：效果字符串
        /// - error：如果验证失败，输出错误信息
        ///
        /// 返回：true 如果有效，false 如果无效。
        ///
        /// 示例错误信息：
        /// - "effect id is empty"
        /// - "effect id 'InvalidId' is not registered"
        /// - "expected 1 arg(s) but got 0"
        /// - "segment 2: effect id 'BadEffect' is not registered"
        /// </summary>
        public static bool TryValidate(string effectString, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(effectString))
            {
                return true;
            }

            if (effectString.Contains("|"))
            {
                var segments = effectString.Split('|');
                for (int i = 0; i < segments.Length; i++)
                {
                    var segment = segments[i].Trim();
                    if (!TryCreateSingle(segment, false, out _, out error))
                    {
                        error = $"segment {i + 1}: {error}";
                        return false;
                    }
                }

                return true;
            }

            return TryCreateSingle(effectString.Trim(), false, out _, out error);
        }

        /// <summary>
        /// 直接执行效果字符串，不需要 CardInstance（用于随机事件等非卡牌来源的效果）。
        /// </summary>
        public static void Execute(string effectString, GameContext context)
        {
            var effect = Create(effectString);
            effect?.Execute(null, context);
        }

        /// <summary>清空所有已注册的效果。通常在测试中使用。</summary>
        public static void ClearAll()
        {
            _registry.Clear();
            CardEffectRegistration.MarkUnregisteredForTesting();
        }

        /// <summary>
        /// 判断效果字符串中是否包含指定效果 ID（按 | 分隔精确匹配 ID 部分，不做裸 Contains）。
        /// 用于额外结算递归防护，替代直接 string.Contains。
        /// </summary>
        public static bool ContainsEffect(string effectString, string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectString) || string.IsNullOrWhiteSpace(effectId))
                return false;
            foreach (var segment in effectString.Split('|'))
            {
                var trimmed = segment.Trim();
                var id = trimmed.Contains(';') ? trimmed.Substring(0, trimmed.IndexOf(';')).Trim() : trimmed;
                if (string.Equals(id, effectId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 内部方法，创建单个效果（不处理 | 分隔）。
        ///
        /// 调用 TryCreateSingle，启用错误日志。返回 effect 对象或 null。
        /// </summary>
        private static ICardEffect CreateSingle(string effectString)
        {
            return TryCreateSingle(effectString, true, out var effect, out _) ? effect : null;
        }

        /// <summary>
        /// 内部方法，尝试创建单个效果，可选择是否记录错误。
        ///
        /// 工作流：
        /// 1. 检查字符串是否为空
        /// 2. 按 ';' 分隔效果 ID 和参数
        /// 3. 在注册表中查找效果 ID 的工厂
        /// 4. 调用工厂创建效果对象
        ///
        /// 参数说明：
        /// - effectString：单个效果字符串（如 "AddMoney;100"，不包含 |）
        /// - logErrors：如果为真，解析失败时记录日志；用于 TryValidate 时禁用日志
        /// - effect：输出参数，返回创建的效果对象
        /// - error：输出参数，返回错误信息（失败时非空）
        ///
        /// 返回：true 如果成功，false 如果失败。
        ///
        /// 错误处理：
        /// - 空字符串 → "effect segment is empty"
        /// - 效果 ID 为空 → "effect id is empty"
        /// - 未注册的效果 ID → "effect id 'X' is not registered"（日志：警告）
        /// - 工厂返回 null → "effect id 'X' returned null"（日志：错误）
        /// - 工厂抛异常 → 异常消息（日志：错误）
        /// </summary>
        private static bool TryCreateSingle(
            string effectString,
            bool logErrors,
            out ICardEffect effect,
            out string error)
        {
            effect = null;
            error = null;

            if (string.IsNullOrWhiteSpace(effectString))
            {
                error = "effect segment is empty";
                return false;
            }

            // 按 ';' 分隔效果 ID 和参数
            // 示例：
            // "AddMoney;100" → parts = ["AddMoney", "100"]
            // "DrawCard;2;EventPool" → parts = ["DrawCard", "2", "EventPool"]
            var parts = effectString.Split(';');
            var effectId = parts[0].Trim();
            if (string.IsNullOrEmpty(effectId))
            {
                error = "effect id is empty";
                return false;
            }

            // 提取参数（从 parts[1] 开始）
            var parameters = new string[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++)
            {
                parameters[i - 1] = parts[i].Trim();
            }

            if (!_registry.TryGetValue(effectId, out var factory))
            {
                error = $"effect id '{effectId}' is not registered";
                if (logErrors)
                {
                    Debug.LogWarning($"[CardEffectFactory] Unregistered effect id: {effectId}");
                }

                return false;
            }

            try
            {
                effect = factory(parameters);
                if (effect == null)
                {
                    error = $"effect id '{effectId}' returned null";
                    if (logErrors)
                    {
                        Debug.LogError($"[CardEffectFactory] Failed to create effect from '{effectString}': {error}");
                    }

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                if (logErrors)
                {
                    Debug.LogError($"[CardEffectFactory] Failed to create effect from '{effectString}': {e.Message}");
                }

                return false;
            }
        }
    }
}

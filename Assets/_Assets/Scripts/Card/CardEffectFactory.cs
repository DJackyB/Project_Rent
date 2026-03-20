using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌效果工厂
    /// 根据效果字符串（如 "AddMoney;100"）解析并创建对应的 ICardEffect 实例。
    /// 使用注册机制，方便扩展新效果。
    /// </summary>
    public static class CardEffectFactory
    {
        private sealed class CompositeCardEffect : ICardEffect
        {
            private readonly List<ICardEffect> _effects;

            public CompositeCardEffect(List<ICardEffect> effects)
            {
                _effects = effects;
            }

            public void Execute(CardInstance source, GameContext context)
            {
                foreach (var effect in _effects)
                {
                    effect?.Execute(source, context);
                }
            }
        }

        /// <summary>
        /// 效果创建器注册表
        /// key: 效果ID（如 "AddMoney"）
        /// value: 工厂函数，接收参数数组并返回 ICardEffect
        /// </summary>
        private static readonly Dictionary<string, Func<string[], ICardEffect>> _registry = new();

        /// <summary>
        /// 注册一个效果创建器
        /// </summary>
        /// <param name="effectId">效果ID，如 "AddMoney"</param>
        /// <param name="factory">创建函数，参数为字符串数组（如 ["100"]）</param>
        public static void Register(string effectId, Func<string[], ICardEffect> factory)
        {
            _registry[effectId] = factory;
        }

        /// <summary>
        /// 根据效果字符串创建效果实例
        /// </summary>
        /// <param name="effectString">效果字符串，格式为 "EffectId;Param1;Param2..."，如 "AddMoney;100"</param>
        /// <returns>ICardEffect 实例，如果解析失败则返回 null</returns>
        public static ICardEffect Create(string effectString)
        {
            if (string.IsNullOrEmpty(effectString))
                return null;

            if (effectString.Contains("|"))
            {
                var segments = effectString.Split('|');
                var effects = new List<ICardEffect>();
                foreach (var segment in segments)
                {
                    var effect = CreateSingle(segment.Trim());
                    if (effect != null)
                    {
                        effects.Add(effect);
                    }
                }

                if (effects.Count == 0) return null;
                if (effects.Count == 1) return effects[0];
                return new CompositeCardEffect(effects);
            }

            return CreateSingle(effectString);
        }

        private static ICardEffect CreateSingle(string effectString)
        {
            if (string.IsNullOrEmpty(effectString))
                return null;

            // 分割效果字符串：EffectId;Param1;Param2...
            var parts = effectString.Split(';');
            var effectId = parts[0].Trim();

            // 提取参数（跳过第一个元素即效果ID）
            var parameters = new string[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++)
            {
                parameters[i - 1] = parts[i].Trim();
            }

            if (_registry.TryGetValue(effectId, out var factory))
            {
                try
                {
                    return factory(parameters);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CardEffectFactory] 创建效果失败: {effectString}, 错误: {e.Message}");
                    return null;
                }
            }

            Debug.LogWarning($"[CardEffectFactory] 未注册的效果ID: {effectId}");
            return null;
        }

        /// <summary>
        /// 清除所有注册（用于测试或重新加载）
        /// </summary>
        public static void ClearAll()
        {
            _registry.Clear();
        }
    }
}

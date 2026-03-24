using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
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

        private static readonly Dictionary<string, Func<string[], ICardEffect>> _registry = new();

        public static void Register(string effectId, Func<string[], ICardEffect> factory)
        {
            _registry[effectId] = factory;
        }

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

        public static void ClearAll()
        {
            _registry.Clear();
        }

        private static ICardEffect CreateSingle(string effectString)
        {
            return TryCreateSingle(effectString, true, out var effect, out _) ? effect : null;
        }

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

            var parts = effectString.Split(';');
            var effectId = parts[0].Trim();
            if (string.IsNullOrEmpty(effectId))
            {
                error = "effect id is empty";
                return false;
            }

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

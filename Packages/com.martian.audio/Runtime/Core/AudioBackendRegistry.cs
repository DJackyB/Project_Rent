using System;
using System.Collections.Generic;
using System.Linq;

namespace Martian.Audio
{
    public static class AudioBackendRegistry
    {
        private static readonly List<IAudioBackendFactory> Factories = new();
        private static bool _defaultFactoriesRegistered;

        public static void Register(IAudioBackendFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            if (Factories.Any(existing => string.Equals(existing.BackendId, factory.BackendId, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            Factories.Add(factory);
        }

        public static void Unregister(IAudioBackendFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            Factories.Remove(factory);
        }

        internal static void EnsureDefaultFactoriesRegistered()
        {
            if (_defaultFactoriesRegistered)
            {
                return;
            }

            _defaultFactoriesRegistered = true;
            Register(new UnityAudioBackendFactory());
        }

        internal static IReadOnlyList<IAudioBackendFactory> GetFactoriesInSelectionOrder(string preferredBackendId)
        {
            EnsureDefaultFactoriesRegistered();

            var ordered = new List<IAudioBackendFactory>();
            if (!string.IsNullOrWhiteSpace(preferredBackendId))
            {
                var preferred = Factories.FirstOrDefault(factory =>
                    string.Equals(factory.BackendId, preferredBackendId, StringComparison.OrdinalIgnoreCase) && factory.IsAvailable);

                if (preferred != null)
                {
                    ordered.Add(preferred);
                }
            }

            foreach (var factory in Factories
                         .Where(factory => factory.IsAvailable)
                         .OrderByDescending(factory => factory.Priority))
            {
                if (ordered.Contains(factory))
                {
                    continue;
                }

                ordered.Add(factory);
            }

            return ordered;
        }

        internal static void ResetForTests()
        {
            Factories.Clear();
            _defaultFactoriesRegistered = false;
        }
    }
}

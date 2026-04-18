using System.Collections.Generic;
using UnityEngine;
using Martian.Tooltip;

namespace Martian.Tooltip.Runtime
{
    public static class TooltipPresenterRegistry
    {
        private static readonly List<ITooltipPresenterFactory> Factories = new();

        public static void Register(ITooltipPresenterFactory factory)
        {
            if (factory == null || Factories.Contains(factory))
            {
                return;
            }

            Factories.Add(factory);
        }

        public static void Unregister(ITooltipPresenterFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            Factories.Remove(factory);
        }

        public static void Clear()
        {
            Factories.Clear();
        }

        public static bool TryCreatePresenter(TooltipContent content, Transform parent, out ITooltipPresenter presenter)
        {
            presenter = null;
            if (content == null || parent == null)
            {
                return false;
            }

            for (int i = 0; i < Factories.Count; i++)
            {
                if (!Factories[i].CanPresent(content))
                {
                    continue;
                }

                presenter = Factories[i].Create(parent);
                return presenter != null;
            }

            return false;
        }
    }
}

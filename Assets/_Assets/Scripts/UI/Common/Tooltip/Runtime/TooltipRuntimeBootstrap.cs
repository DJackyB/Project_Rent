using UnityEngine;

namespace BaoZuPo.UI.Common.Tooltip.Runtime
{
    public static class TooltipRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            TooltipRuntimeService.EnsureInstance();
        }
    }
}

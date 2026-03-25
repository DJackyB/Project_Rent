using Martian.Tooltip.Presets;
using Martian.Tooltip.Runtime;

namespace BaoZuPo.Integration.Martian.Tooltip
{
    public static class BaoZuPoMartianTooltipIntegration
    {
        public static void Install()
        {
            TooltipRuntimeInstaller.Install();
            TooltipDocumentPresetInstaller.Install();
            BaoZuPoCardTooltipPresenterRegistration.Install();
        }
    }
}

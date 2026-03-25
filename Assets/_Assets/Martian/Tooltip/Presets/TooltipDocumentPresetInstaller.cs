using Martian.Tooltip.Runtime;

namespace Martian.Tooltip.Presets
{
    public static class TooltipDocumentPresetInstaller
    {
        private static readonly TooltipDocumentPresenterFactory Factory = new();

        public static void Install()
        {
            TooltipPresenterRegistry.Register(Factory);
        }
    }
}

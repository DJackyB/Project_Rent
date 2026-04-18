namespace Martian.Tooltip.Runtime
{
    public static class TooltipRuntimeInstaller
    {
        public static TooltipRuntimeService Install()
        {
            return TooltipRuntimeService.EnsureInstance();
        }
    }
}

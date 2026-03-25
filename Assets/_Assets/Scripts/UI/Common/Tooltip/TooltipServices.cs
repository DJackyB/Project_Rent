namespace BaoZuPo.UI.Common.Tooltip
{
    public static class TooltipServices
    {
        private static readonly NullTooltipService NullService = new();
        private static ITooltipService _current;

        public static ITooltipService Current => _current ?? NullService;

        public static void SetCurrent(ITooltipService service)
        {
            _current = service;
        }

        public static void ResetCurrent(ITooltipService service)
        {
            if (ReferenceEquals(_current, service))
            {
                _current = null;
            }
        }
    }
}

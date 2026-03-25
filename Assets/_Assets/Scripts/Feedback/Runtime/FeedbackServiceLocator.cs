using BaoZuPo.Feedback.Core;

namespace BaoZuPo.Feedback.Runtime
{
    public static class FeedbackServiceLocator
    {
        private static IFeedbackService _current;

        public static IFeedbackService Current
        {
            get
            {
                _current ??= NoOpFeedbackService.Instance;
                return _current;
            }
        }

        public static void SetService(IFeedbackService service)
        {
            _current = service ?? NoOpFeedbackService.Instance;
        }

        public static void Reset()
        {
            _current = NoOpFeedbackService.Instance;
        }
    }
}

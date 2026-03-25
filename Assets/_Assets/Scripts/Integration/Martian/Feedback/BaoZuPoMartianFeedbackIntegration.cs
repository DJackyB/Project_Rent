using BaoZuPo.Core;
using Martian.Feedback;
using Martian.Feedback.Runtime;

namespace BaoZuPo.Integration.Martian.Feedback
{
    public static class BaoZuPoMartianFeedbackIntegration
    {
        public static bool MoneyFeedbackEnabled { get; private set; } = true;

        public static void Configure(FeedbackBootstrap bootstrap, GameConfig config)
        {
            MoneyFeedbackEnabled = config == null || config.enableMoneyFeedback;

            if (bootstrap == null)
            {
                return;
            }

            bootstrap.Configure(CreateRuntimeOptions(config));
        }

        public static FeedbackRuntimeOptions CreateRuntimeOptions(GameConfig config)
        {
            var options = new FeedbackRuntimeOptions();
            if (config == null)
            {
                return options;
            }

            options.EnableFeedback = config.enableFeedback;
            options.EnableMoneyFeedback = config.enableMoneyFeedback;
            options.EnableLogs = config.enableFeedbackLogs;
            return options;
        }
    }
}

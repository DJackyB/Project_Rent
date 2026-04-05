using BaoZuPo.Core;
using Martian.Feedback;
using Martian.Feedback.Runtime;
using Martian.Localization;

namespace BaoZuPo.Integration.Martian.Feedback
{
    /// <summary>
    /// 反馈系统配置集成。
    /// 从 GameConfig 中读取反馈模块的开关，配置 FeedbackBootstrap。
    ///
    /// 使用场景：
    /// - 游戏初始化时调用 Configure() 以应用 GameConfig 中的反馈设置
    /// - 其他模块通过 MoneyFeedbackEnabled 来判断是否显示金钱反馈
    /// </summary>
    public static class BaoZuPoMartianFeedbackIntegration
    {
        /// <summary>
        /// 金钱反馈是否启用。用于在发布反馈时判断是否发送。
        /// </summary>
        public static bool MoneyFeedbackEnabled { get; private set; } = true;

        /// <summary>
        /// 配置 FeedbackBootstrap 的运行时选项。
        /// 通常由游戏初始化流程调用。
        /// </summary>
        public static void Configure(FeedbackBootstrap bootstrap, GameConfig config)
        {
            MoneyFeedbackEnabled = config == null || config.enableMoneyFeedback;

            if (bootstrap == null)
            {
                return;
            }

            bootstrap.SetFontResolver(ResolveFeedbackFont);
            bootstrap.Configure(CreateRuntimeOptions(config));
        }

        /// <summary>
        /// 从 GameConfig 创建 FeedbackRuntimeOptions。
        /// 将 GameConfig 的反馈设置映射到 Martian.Feedback 的选项对象。
        /// </summary>
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

        private static TMPro.TMP_FontAsset ResolveFeedbackFont()
        {
            try
            {
                return LocalizationFontUtility.GetPreferredFontAsset();
            }
            catch
            {
                return null;
            }
        }
    }
}

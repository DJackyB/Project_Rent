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
            options.DefaultScreenOffset = new UnityEngine.Vector2(0f, 114f);
            options.SequenceScreenOffset = new UnityEngine.Vector2(0f, 108f);
            options.PanelSize = new UnityEngine.Vector2(224f, 70f);
            options.PanelPadding = new UnityEngine.Vector2(28f, 14f);
            options.PanelColor = new UnityEngine.Color(0.07f, 0.1f, 0.16f, 0.88f);
            options.TextColor = UnityEngine.Color.white;
            options.PositiveColor = new UnityEngine.Color(1f, 0.84f, 0.34f, 1f);
            options.CostColor = new UnityEngine.Color(1f, 0.62f, 0.38f, 1f);
            options.LoanColor = new UnityEngine.Color(1f, 0.46f, 0.38f, 1f);
            options.FinalColor = new UnityEngine.Color(1f, 0.93f, 0.62f, 1f);
            options.MultiplierColor = new UnityEngine.Color(0.78f, 0.88f, 1f, 1f);
            options.SingleHoldSeconds = 0.48f;
            options.NormalHoldSeconds = 0.46f;
            options.FinalHoldSeconds = 0.6f;
            options.SingleFadeInSeconds = 0.11f;
            options.SingleFadeOutSeconds = 0.14f;
            options.SingleScale = 1.04f;
            options.FinalScale = 1.12f;
            options.SequenceGapSeconds = 0.04f;
            options.SequenceFadeInSeconds = 0.11f;
            options.SequenceFadeOutSeconds = 0.14f;
            if (config == null)
            {
                return options;
            }

            options.EnableFeedback = config.enableFeedback;
            options.EnableMoneyFeedback = config.enableMoneyFeedback;
            options.EnableLogs = config.enableFeedbackLogs;
            return options;
        }

        /// <summary>
        /// 创建默认浮动文本后端，并复用项目本地化字体解析。
        /// Feel 组合后端会替换 Coordinator 默认后端，因此主后端必须显式继承这份配置。
        /// </summary>
        public static FloatingTextFeedbackBackend CreateDefaultFloatingTextBackend()
        {
            var backend = new FloatingTextFeedbackBackend();
            backend.SetFontResolver(ResolveFeedbackFont);
            return backend;
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

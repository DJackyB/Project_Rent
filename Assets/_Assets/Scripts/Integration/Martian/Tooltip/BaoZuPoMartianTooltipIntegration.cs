using Martian.Tooltip.Presets;
using Martian.Tooltip.Runtime;

namespace BaoZuPo.Integration.Martian.Tooltip
{
    /// <summary>
    /// 提示系统集成。
    /// 安装 Martian.Tooltip 运行时所需的组件和自定义呈现器。
    ///
    /// 使用：
    /// - 游戏初始化时调用 Install()，以完成提示系统的全面安装
    /// - 注册卡牌预览呈现器，使悬停卡牌时显示卡牌预览
    /// </summary>
    public static class BaoZuPoMartianTooltipIntegration
    {
        /// <summary>
        /// 完整安装提示系统。
        /// 执行以下步骤：
        /// 1. 安装 Martian.Tooltip 运行时
        /// 2. 安装预设文档（UI 布局）
        /// 3. 注册游戏自定义的卡牌预览呈现器
        /// </summary>
        public static void Install()
        {
            TooltipRuntimeInstaller.Install();
            TooltipDocumentPresetInstaller.Install();
            BaoZuPoCardTooltipPresenterRegistration.Install();
        }
    }
}

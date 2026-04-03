namespace Martian.Tooltip
{
    /// <summary>
    /// 提示框内容提供者接口。
    /// 任何 UI 元素都可以实现此接口来提供提示内容。
    ///
    /// 使用场景：
    /// - 卡牌按钮实现此接口返回卡牌说明
    /// - 技能图标实现此接口返回技能描述
    /// - 任何可点击元素都可以有提示
    ///
    /// 实现建议：
    /// - 通过 TooltipTrigger 组件在 OnPointerEnter/Exit 时调用
    /// - 内容可来自本地字符串、配置资产或数据库
    /// </summary>
    public interface ITooltipContentProvider
    {
        /// <summary>
        /// 尝试构建提示请求。
        /// </summary>
        /// <param name="request">
        /// 若成功，填入包含内容、锚点等信息的请求对象。
        /// 若无法提供内容，设为 null。
        /// </param>
        /// <returns>成功构建提示返回 true</returns>
        bool TryBuildTooltipRequest(out TooltipRequest request);
    }
}

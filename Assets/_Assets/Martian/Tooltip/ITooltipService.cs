using UnityEngine;

namespace Martian.Tooltip
{
    /// <summary>
    /// 提示框服务接口。负责显示和隐藏玩家交互时的提示信息。
    ///
    /// 主要职责：
    /// - Show()：显示提示（如卡牌说明、技能描述等）
    /// - Hide()：按所有者隐藏提示（防止其他组件误隐藏）
    /// - HideAll()：隐藏所有当前提示
    ///
    /// 实现（TooltipRuntimeService）：
    /// - 单例模式，自动在首次使用时创建
    /// - 使用 ITooltipContentProvider 获取内容
    /// - 使用 ITooltipPresenter 实际渲染提示
    /// - 支持跟随指针或固定在锚点两种模式
    /// </summary>
    public interface ITooltipService
    {
        /// <summary>服务是否可用。</summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 显示提示框。
        /// </summary>
        /// <param name="request">提示请求，包含内容、锚点、位置等</param>
        /// <param name="pointerPosition">可选：指针位置（用于跟随指针模式）</param>
        void Show(TooltipRequest request, Vector2? pointerPosition = null);

        /// <summary>
        /// 隐藏指定所有者的提示。只有所有者一致的提示才会被隐藏。
        /// </summary>
        /// <param name="owner">提示的所有者对象</param>
        void Hide(object owner);

        /// <summary>
        /// 隐藏所有当前活跃的提示。
        /// </summary>
        void HideAll();
    }
}

using Martian.Feedback;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Martian.Feedback.Runtime
{
    /// <summary>
    /// 反馈系统启动引导程序。负责初始化和配置反馈服务链。
    ///
    /// 职责：
    /// - 在 OnEnable 时注册全局反馈服务
    /// - 创建并管理 FeedbackPlaybackCoordinator
    /// - 支持运行时配置和后端切换
    /// - 在 OnDisable 时清理资源
    ///
    /// 使用流程：
    /// 1. 将此组件放在场景中的一个 GameObject 上（如 UI Canvas 的子节点）
    /// 2. 在 Inspector 中配置 FeedbackRuntimeOptions（启用状态、排序顺序等）
    /// 3. 运行时可通过 Configure() 或 SetBackend() 改变配置
    /// 4. FeedbackServiceLocator 将自动指向这个启动程序的 Coordinator
    /// </summary>
    [MovedFrom("BaoZuPo.Feedback.Runtime")]
    [DisallowMultipleComponent]
    public class FeedbackBootstrap : MonoBehaviour
    {
        /// <summary>反馈运行时配置选项（在 Inspector 中编辑）。</summary>
        [SerializeField] private FeedbackRuntimeOptions options = new();

        /// <summary>可选的后端覆盖。如果设置，将使用该后端而非默认的浮动文本后端。</summary>
        private IFeedbackPlaybackBackend _backendOverride;

        /// <summary>协调器实例，实际执行反馈发布。</summary>
        private FeedbackPlaybackCoordinator _coordinator;

        /// <summary>全局活跃的 FeedbackBootstrap 实例。</summary>
        public static FeedbackBootstrap Active { get; private set; }

        /// <summary>访问协调器，用于直接发布反馈或获取状态。</summary>
        public FeedbackPlaybackCoordinator Coordinator => _coordinator;

        /// <summary>反馈运行时是否可用。</summary>
        public bool IsRuntimeAvailable => _coordinator != null && _coordinator.IsAvailable;

        /// <summary>
        /// 启用事件：注册为全局活跃实例，并绑定反馈服务。
        /// </summary>
        private void OnEnable()
        {
            Active = this;
            RebindService();
        }

        /// <summary>
        /// 禁用事件：清理资源并重置服务定位器。
        /// </summary>
        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }

            _coordinator?.Clear();
            FeedbackServiceLocator.Reset();
        }

        /// <summary>
        /// 运行时配置选项。用于改变反馈的启用状态、排序顺序等。
        /// </summary>
        /// <param name="runtimeOptions">新的运行时选项对象</param>
        public void Configure(FeedbackRuntimeOptions runtimeOptions)
        {
            options = runtimeOptions != null ? runtimeOptions.Clone() : new FeedbackRuntimeOptions();
            RebindService();
        }

        /// <summary>
        /// 运行时切换反馈后端实现。
        /// 例如：从浮动文本切换到自定义音效后端。
        /// </summary>
        /// <param name="backend">新的后端实现，或 null 使用默认后端</param>
        public void SetBackend(IFeedbackPlaybackBackend backend)
        {
            _backendOverride = backend;
            RebindService();
        }

        /// <summary>
        /// 重新绑定服务链。
        /// 流程：
        /// 1. 检查和初始化选项对象
        /// 2. 如果反馈禁用，清理协调器并重置服务定位器
        /// 3. 否则，确保协调器存在
        /// 4. 配置协调器的选项和后端
        /// 5. 将协调器注册到全局服务定位器
        /// </summary>
        private void RebindService()
        {
            if (options == null)
            {
                options = new FeedbackRuntimeOptions();
            }

            if (!options.EnableFeedback)
            {
                _coordinator?.Clear();
                FeedbackServiceLocator.Reset();
                return;
            }

            EnsureCoordinator();
            if (_coordinator == null)
            {
                FeedbackServiceLocator.Reset();
                return;
            }

            _coordinator.Configure(options);
            _coordinator.SetBackend(_backendOverride);
            FeedbackServiceLocator.SetService(_coordinator);
        }

        /// <summary>
        /// 确保协调器存在。
        /// 如果不存在，在本 Bootstrap 下创建 "FeedbackPlaybackCoordinator" 子对象并加入组件。
        /// </summary>
        private void EnsureCoordinator()
        {
            if (_coordinator != null)
            {
                return;
            }

            var coordinatorTransform = transform.Find("FeedbackPlaybackCoordinator");
            if (coordinatorTransform == null)
            {
                coordinatorTransform = new GameObject("FeedbackPlaybackCoordinator", typeof(RectTransform)).transform;
                coordinatorTransform.SetParent(transform, false);
            }

            _coordinator = coordinatorTransform.GetComponent<FeedbackPlaybackCoordinator>();
            if (_coordinator == null)
            {
                _coordinator = coordinatorTransform.gameObject.AddComponent<FeedbackPlaybackCoordinator>();
            }
        }
    }
}

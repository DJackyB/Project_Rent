using BaoZuPo.Core;
using BaoZuPo.Feedback.Core;
using UnityEngine;

namespace BaoZuPo.Feedback.Runtime
{
    public class FeedbackBootstrap : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private bool enableFeedback = true;
        [SerializeField] private bool enableMoneyFeedback = true;
        [SerializeField] private bool enableFeedbackLogs = true;

        private FeedbackPlaybackCoordinator _coordinator;

        public static FeedbackBootstrap Active { get; private set; }

        public FeedbackPlaybackCoordinator Coordinator => _coordinator;

        public bool IsRuntimeAvailable => _coordinator != null && _coordinator.IsAvailable;

        private void OnEnable()
        {
            Active = this;
            RebindService();
        }

        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }

            FeedbackServiceLocator.Reset();
        }

        public void Configure(GameConfig config)
        {
            if (config != null)
            {
                enableFeedback = config.enableFeedback;
                enableMoneyFeedback = config.enableMoneyFeedback;
                enableFeedbackLogs = config.enableFeedbackLogs;
            }

            RebindService();
        }

        private void RebindService()
        {
            if (!enableFeedback || !enableMoneyFeedback)
            {
                FeedbackServiceLocator.Reset();
                return;
            }

            EnsureCoordinator();
            if (_coordinator == null)
            {
                FeedbackServiceLocator.Reset();
                return;
            }

            _coordinator.Configure(enableFeedbackLogs);
            FeedbackServiceLocator.SetService(_coordinator);
        }

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

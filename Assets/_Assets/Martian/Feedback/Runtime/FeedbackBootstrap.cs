using Martian.Feedback;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Martian.Feedback.Runtime
{
    [MovedFrom("BaoZuPo.Feedback.Runtime")]
    [DisallowMultipleComponent]
    public class FeedbackBootstrap : MonoBehaviour
    {
        [SerializeField] private FeedbackRuntimeOptions options = new();

        private IFeedbackPlaybackBackend _backendOverride;
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

            _coordinator?.Clear();
            FeedbackServiceLocator.Reset();
        }

        public void Configure(FeedbackRuntimeOptions runtimeOptions)
        {
            options = runtimeOptions != null ? runtimeOptions.Clone() : new FeedbackRuntimeOptions();
            RebindService();
        }

        public void SetBackend(IFeedbackPlaybackBackend backend)
        {
            _backendOverride = backend;
            RebindService();
        }

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

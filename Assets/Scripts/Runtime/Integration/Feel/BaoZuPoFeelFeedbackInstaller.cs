using System;
using System.Collections.Generic;
using BaoZuPo.Integration.Martian.Feedback;
using Martian.Feedback.Runtime;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace BaoZuPo.Integration.Feel
{
    /// <summary>
    /// Scene-level installer that wires Feel as an optional visual-only secondary feedback backend.
    /// </summary>
    [AddComponentMenu("BaoZuPo/Feel Feedback Installer")]
    public sealed class BaoZuPoFeelFeedbackInstaller : MonoBehaviour
    {
        /// <summary>Current scene-level installer used by UI code that cannot hold an explicit reference.</summary>
        public static BaoZuPoFeelFeedbackInstaller Active { get; private set; }

        [Serializable]
        private struct SlotEntry
        {
            public string slot;
            public MMF_Player player;
        }

        [Header("Required")]
        [SerializeField] private FeedbackBootstrap _bootstrap;

        [Header("Slot Bindings")]
        [SerializeField] private List<SlotEntry> _slotBindings = new();

        private FeelFeedbackBackend _feelBackend;
        private bool _backendInstalled;

        public FeelFeedbackBackend FeelBackend => _feelBackend;

        private void OnEnable()
        {
            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
            UninstallBackend();
        }

        private void Awake()
        {
            if (_bootstrap == null)
            {
                Debug.LogError("[FeelInstaller] FeedbackBootstrap is not assigned. Feel integration has been disabled.");
                enabled = false;
            }
        }

        private void Start()
        {
            _feelBackend = new FeelFeedbackBackend();
            _feelBackend.Attach(transform);

            foreach (var entry in _slotBindings)
                _feelBackend.RegisterPlayer(entry.slot, entry.player);

            var primary = BaoZuPoMartianFeedbackIntegration.CreateDefaultFloatingTextBackend();
            var composite = new CompositeFeedbackPlaybackBackend(primary, _feelBackend);
            _bootstrap.SetBackend(composite);
            _backendInstalled = true;
        }

        private void OnDestroy()
        {
            UninstallBackend();
        }

        private void UninstallBackend()
        {
            if (!_backendInstalled)
            {
                _feelBackend = null;
                return;
            }

            _feelBackend?.Clear();
            if (_bootstrap != null && _bootstrap.isActiveAndEnabled && _bootstrap.gameObject.activeInHierarchy)
            {
                _bootstrap.SetBackend(null);
            }

            _backendInstalled = false;
            _feelBackend = null;
        }
    }
}

using BaoZuPo.Core;
using BaoZuPo.Feedback.Adapters.BaoZuPo;
using BaoZuPo.Feedback.Runtime;
using Martian.EventBus;
using UnityEngine;

namespace BaoZuPo.UI.Settlement
{
    public class UISettlementSequenceController : MonoBehaviour
    {
        private FeedbackPlaybackCoordinator _boundCoordinator;
        private bool _pendingRefresh;

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.SettlementSequenceQueued>(OnSettlementQueued);
            EventBus.Subscribe<GameEvents.TurnEnded>(OnTurnEnded);
            BindCoordinator();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.SettlementSequenceQueued>(OnSettlementQueued);
            EventBus.Unsubscribe<GameEvents.TurnEnded>(OnTurnEnded);
            UnbindCoordinator();
            _pendingRefresh = false;
        }

        private void OnSettlementQueued(GameEvents.SettlementSequenceQueued payload)
        {
            BindCoordinator();
            BaoZuPoFeedbackAdapter.PublishSettlementSequence(payload);

            if (!FeedbackServiceLocator.Current.IsAvailable)
            {
                _pendingRefresh = true;
                return;
            }

            _pendingRefresh = true;
        }

        private void OnTurnEnded(GameEvents.TurnEnded _)
        {
            if (_pendingRefresh)
            {
                if (!FeedbackServiceLocator.Current.IsAvailable)
                {
                    _pendingRefresh = false;
                    UIManager.Instance?.RefreshAll();
                }

                return;
            }

            UIManager.Instance?.RefreshAll();
        }

        private void OnAllPlaybackCompleted()
        {
            if (!_pendingRefresh)
            {
                return;
            }

            _pendingRefresh = false;
            UIManager.Instance?.RefreshAll();
        }

        private void BindCoordinator()
        {
            var coordinator = FeedbackBootstrap.Active != null ? FeedbackBootstrap.Active.Coordinator : null;
            if (coordinator == _boundCoordinator)
            {
                return;
            }

            UnbindCoordinator();
            _boundCoordinator = coordinator;
            if (_boundCoordinator != null)
            {
                _boundCoordinator.AllPlaybackCompleted += OnAllPlaybackCompleted;
            }
        }

        private void UnbindCoordinator()
        {
            if (_boundCoordinator == null)
            {
                return;
            }

            _boundCoordinator.AllPlaybackCompleted -= OnAllPlaybackCompleted;
            _boundCoordinator = null;
        }
    }
}

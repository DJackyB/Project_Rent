using BaoZuPo.Integration.Martian.Feedback;
using Martian.Feedback.Runtime;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace BaoZuPo.Integration.Feel
{
    /// <summary>
    /// Scene-level installer that wires Feel as a visual-only secondary feedback backend.
    /// </summary>
    [AddComponentMenu("BaoZuPo/Feel Feedback Installer")]
    public sealed class BaoZuPoFeelFeedbackInstaller : MonoBehaviour
    {
        [Header("Required")]
        [SerializeField] private FeedbackBootstrap _bootstrap;

        [Header("Feel Players (Optional)")]
        [SerializeField] private MMF_Player _moneyDeltaPlayer;
        [SerializeField] private MMF_Player _settlementStepPlayer;
        [SerializeField] private MMF_Player _loanPaymentPlayer;
        [SerializeField] private MMF_Player _cardPlayPlayer;
        [SerializeField] private MMF_Player _rewardRevealPlayer;

        private FeelFeedbackBackend _feelBackend;

        public FeelFeedbackBackend FeelBackend => _feelBackend;

        private void Awake()
        {
            if (_bootstrap == null)
            {
                Debug.LogError("[FeelInstaller] FeedbackBootstrap is not assigned. Feel integration has been disabled.");
                enabled = false;
                return;
            }

            ValidatePlayerReferences();
        }

        private void Start()
        {
            _feelBackend = new FeelFeedbackBackend();
            RegisterPlayers();

            var primary = BaoZuPoMartianFeedbackIntegration.CreateDefaultFloatingTextBackend();
            var composite = new CompositeFeedbackPlaybackBackend(primary, _feelBackend);
            _bootstrap.SetBackend(composite);
        }

        private void OnDestroy()
        {
            // Do not restore the default backend during teardown. Rebinding here can create
            // new runtime feedback objects while the scene is already being destroyed.
            _feelBackend = null;
        }

        private void RegisterPlayers()
        {
            _feelBackend.RegisterPlayer(FeelFeedbackSlots.MoneyDelta, _moneyDeltaPlayer);
            _feelBackend.RegisterPlayer(FeelFeedbackSlots.SettlementStep, _settlementStepPlayer);
            _feelBackend.RegisterPlayer(FeelFeedbackSlots.LoanPayment, _loanPaymentPlayer);
            _feelBackend.RegisterPlayer(FeelFeedbackSlots.CardPlay, _cardPlayPlayer);
            _feelBackend.RegisterPlayer(FeelFeedbackSlots.RewardReveal, _rewardRevealPlayer);
        }

        private void ValidatePlayerReferences()
        {
            CheckPlayer(nameof(_moneyDeltaPlayer), _moneyDeltaPlayer);
            CheckPlayer(nameof(_settlementStepPlayer), _settlementStepPlayer);
            CheckPlayer(nameof(_loanPaymentPlayer), _loanPaymentPlayer);
            CheckPlayer(nameof(_cardPlayPlayer), _cardPlayPlayer);
            CheckPlayer(nameof(_rewardRevealPlayer), _rewardRevealPlayer);
        }

        private static void CheckPlayer(string fieldName, MMF_Player player)
        {
            if (player == null)
            {
                Debug.LogWarning($"[FeelInstaller] {fieldName} is not assigned. That Feel slot will stay inactive.");
            }
        }
    }
}

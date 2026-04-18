using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.GameFlow;
using Martian.EventBus;
using UnityEngine;

namespace BaoZuPo.UI
{
    /// <summary>
    /// Fixed-position shop card slot.
    /// The slot keeps one generated shop card between turns until the player uses it.
    /// Playing the card opens the shop through the card effect chain instead of a dedicated button.
    /// </summary>
    public class UIShopCardSlot : MonoBehaviour
    {
        [SerializeField] private GameObject _slotRoot;
        [SerializeField] private UICardView _cardView;
        [SerializeField] private GameObject cardPrefab;

        private CardInstance _displayInstance;
        private bool _loggedMissingSlotRoot;
        private bool _loggedMissingCardPrefab;
        private bool _loggedMissingCardView;
        private bool _loggedMissingShopCardConfig;

        private void Start()
        {
            HideSlotVisualImmediately();
            SyncForCurrentTurnIfNeeded();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Subscribe<GameEvents.ShopOpened>(OnShopOpened);
            EventBus.Subscribe<GameEvents.ShopClosed>(OnShopClosed);
            EventBus.Subscribe<GameEvents.CardPlayed>(OnCardPlayed);

            SyncForCurrentTurnIfNeeded();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Unsubscribe<GameEvents.ShopOpened>(OnShopOpened);
            EventBus.Unsubscribe<GameEvents.ShopClosed>(OnShopClosed);
            EventBus.Unsubscribe<GameEvents.CardPlayed>(OnCardPlayed);
        }

        private void OnTurnStarted(GameEvents.TurnStarted _)
        {
            RefreshSlotForCurrentTurn();
        }

        private void OnShopOpened(GameEvents.ShopOpened _)
        {
            SetSlotVisible(false);
        }

        private void OnShopClosed(GameEvents.ShopClosed _)
        {
            SetSlotVisible(HasDisplayCard());
        }

        private void OnCardPlayed(GameEvents.CardPlayed e)
        {
            if (!ReferenceEquals(e.Card, _displayInstance))
            {
                return;
            }

            _displayInstance = null;
            RefreshCardView();
            SetSlotVisible(false);
        }

        private void EnsureCardViewReady()
        {
            if (_cardView != null)
            {
                return;
            }

            if (_slotRoot == null)
            {
                LogMissingSlotRootOnce();
                return;
            }

            if (_slotRoot != null)
            {
                _cardView = _slotRoot.GetComponentInChildren<UICardView>(true);
            }

            if (_cardView != null)
            {
                return;
            }

            if (cardPrefab == null)
            {
                LogMissingCardPrefabOnce();
                return;
            }

            bool useExistingSceneObject = cardPrefab.transform.IsChildOf(_slotRoot.transform);
            if (useExistingSceneObject)
            {
                _cardView = cardPrefab.GetComponent<UICardView>();
                if (_cardView == null)
                {
                    LogMissingCardViewOnce("Configured cardPrefab exists under _slotRoot but does not contain UICardView.");
                }
                return;
            }

            var cardObject = Instantiate(cardPrefab, _slotRoot.transform, false);
            _cardView = cardObject.GetComponent<UICardView>();
            if (_cardView == null)
            {
                LogMissingCardViewOnce("Instantiated cardPrefab does not contain UICardView.");
            }
        }

        private void EnsureDisplayInstanceForTurn()
        {
            if (_displayInstance != null)
            {
                return;
            }

            var config = GameManager.Instance != null ? GameManager.Instance.gameConfig : null;
            if (config == null || config.shopCard == null)
            {
                LogMissingShopCardConfigOnce();
                return;
            }

            _displayInstance = new CardInstance(config.shopCard);
            _displayInstance.ConfigureAsTemporaryHandCard();
        }

        private void RefreshCardView()
        {
            if (_cardView == null)
            {
                LogMissingCardViewOnce("UIShopCardSlot could not resolve a UICardView for the shop slot.");
                return;
            }

            bool hasDisplayCard = HasDisplayCard();
            _cardView.gameObject.SetActive(hasDisplayCard);
            if (!hasDisplayCard)
            {
                return;
            }

            _cardView.Setup(_displayInstance, CardViewContext.Hand, null);
            _cardView.RefreshViewState();
        }

        private bool HasDisplayCard()
        {
            return _displayInstance != null && !_displayInstance.IsDestroyed;
        }

        private void SetSlotVisible(bool visible)
        {
            if (ReferencesSelfSlotRoot())
            {
                if (_cardView != null)
                {
                    _cardView.gameObject.SetActive(visible && HasDisplayCard());
                }
                else if (!visible)
                {
                    LogSelfSlotRootUsageOnce();
                }

                return;
            }

            if (_slotRoot != null)
            {
                _slotRoot.SetActive(visible);
                return;
            }

            LogMissingSlotRootOnce();
        }

        private void SyncForCurrentTurnIfNeeded()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (TurnManager.Instance == null)
            {
                return;
            }

            if (TurnManager.Instance.CurrentTurn <= 0)
            {
                return;
            }

            RefreshSlotForCurrentTurn();
        }

        private void RefreshSlotForCurrentTurn()
        {
            EnsureCardViewReady();
            EnsureDisplayInstanceForTurn();
            RefreshCardView();
            SetSlotVisible(HasDisplayCard() && (TurnManager.Instance == null || !TurnManager.Instance.IsShopOpen));
        }

        private void HideSlotVisualImmediately()
        {
            if (ReferencesSelfSlotRoot())
            {
                if (_cardView != null)
                {
                    _cardView.gameObject.SetActive(false);
                }

                return;
            }

            if (_slotRoot != null)
            {
                _slotRoot.SetActive(false);
            }
        }

        private bool ReferencesSelfSlotRoot()
        {
            return _slotRoot != null && ReferenceEquals(_slotRoot, gameObject);
        }

        private void LogMissingSlotRootOnce()
        {
            if (_loggedMissingSlotRoot)
            {
                return;
            }

            _loggedMissingSlotRoot = true;
            Debug.LogWarning("[UIShopCardSlot] _slotRoot is not assigned. The fixed shop card slot cannot be shown.", this);
        }

        private void LogMissingCardPrefabOnce()
        {
            if (_loggedMissingCardPrefab)
            {
                return;
            }

            _loggedMissingCardPrefab = true;
            Debug.LogWarning("[UIShopCardSlot] cardPrefab is not assigned and no existing UICardView was found under _slotRoot.", this);
        }

        private void LogMissingCardViewOnce(string message)
        {
            if (_loggedMissingCardView)
            {
                return;
            }

            _loggedMissingCardView = true;
            Debug.LogWarning($"[UIShopCardSlot] {message}", this);
        }

        private void LogMissingShopCardConfigOnce()
        {
            if (_loggedMissingShopCardConfig)
            {
                return;
            }

            _loggedMissingShopCardConfig = true;
            Debug.LogWarning("[UIShopCardSlot] GameConfig.shopCard is missing, so the shop slot cannot generate its persistent shop card.", this);
        }

        private void LogSelfSlotRootUsageOnce()
        {
            if (_loggedMissingCardView)
            {
                return;
            }

            _loggedMissingCardView = true;
            Debug.LogWarning("[UIShopCardSlot] _slotRoot points to the same GameObject as UIShopCardSlot. Keeping the host object active and hiding only the card view.", this);
        }
    }
}

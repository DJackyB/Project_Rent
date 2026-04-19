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
        [SerializeField] private GameObject cardPrefab;

        private CardInstance _displayInstance;
        private UICardView _cardView;

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

            if (_slotRoot == null || cardPrefab == null)
            {
                return;
            }

            _cardView = _slotRoot.GetComponentInChildren<UICardView>(true);
            if (_cardView != null)
            {
                return;
            }

            var cardObject = Instantiate(cardPrefab, _slotRoot.transform, false);
            _cardView = cardObject.GetComponent<UICardView>();
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
                return;
            }

            _displayInstance = new CardInstance(config.shopCard);
        }

        private void RefreshCardView()
        {
            bool hasDisplayCard = HasDisplayCard();
            if (_cardView != null)
            {
                _cardView.gameObject.SetActive(hasDisplayCard);
            }

            if (!hasDisplayCard)
            {
                return;
            }

            if (_cardView == null)
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
            if (_slotRoot != null)
            {
                _slotRoot.SetActive(visible);
            }
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
            if (_slotRoot != null)
            {
                _slotRoot.SetActive(false);
            }
        }
    }
}

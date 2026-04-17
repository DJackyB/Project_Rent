using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.GameFlow;
using Martian.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 固定位置的商店卡槽。
    /// 每回合开始时显示商店卡，玩家点击后打开商店并隐藏卡片。
    /// 位置由 Inspector 中的 RectTransform 配置。
    /// </summary>
    public class UIShopCardSlot : MonoBehaviour
    {
        [SerializeField] private GameObject _slotRoot;
        [SerializeField] private UICardView _cardView;
        [SerializeField] private Button _shopButton;

        private CardInstance _displayInstance;

        private void Start()
        {
            _shopButton.onClick.AddListener(OnShopButtonClicked);
            _slotRoot.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Subscribe<GameEvents.ShopOpened>(OnShopOpened);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Unsubscribe<GameEvents.ShopOpened>(OnShopOpened);
        }

        private void OnTurnStarted(GameEvents.TurnStarted _)
        {
            EnsureCardViewSetup();
            _slotRoot.SetActive(_displayInstance != null);
        }

        private void OnShopOpened(GameEvents.ShopOpened _)
        {
            _slotRoot.SetActive(false);
        }

        private void OnShopButtonClicked()
        {
            TurnManager.Instance?.OpenShop(null);
        }

        private void EnsureCardViewSetup()
        {
            if (_displayInstance != null) return;
            if (_cardView == null) return;

            var config = GameManager.Instance != null ? GameManager.Instance.gameConfig : null;
            if (config == null || config.shopCard == null) return;

            _displayInstance = new CardInstance(config.shopCard);
            _cardView.Setup(_displayInstance, CardViewContext.RewardPick);
        }
    }
}

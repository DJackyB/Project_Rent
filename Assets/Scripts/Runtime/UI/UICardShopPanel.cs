using System;
using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using DG.Tweening;
using Martian.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// Shop panel for the temporary shop card flow.
    /// </summary>
    public class UICardShopPanel : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject _panelRoot;

        [Header("Title")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Header("Card Slots")]
        [SerializeField] private Transform _cardSlot0;
        [SerializeField] private Transform _cardSlot1;
        [SerializeField] private Transform _cardSlot2;

        [Header("Card Prefab")]
        [SerializeField] private GameObject _cardPrefab;

        [Header("Close Button")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _closeButtonText;

        [Header("Motion")]
        [SerializeField] private float _cardScale = 2f;
        [SerializeField] private float _revealDuration = 0.2f;
        [SerializeField] private float _revealStagger = 0.06f;
        [SerializeField] private float _revealYOffset = 24f;
        [SerializeField] private float _purchaseDuration = 0.16f;
        [SerializeField] private float _disabledAlpha = 0.45f;

        private readonly List<ShopEntry> _entries = new();
        private CardData[] _options = Array.Empty<CardData>();
        private bool _purchaseLocked;
        private Sequence _revealSequence;

        private sealed class ShopEntry
        {
            public int Index;
            public CardData Card;
            public GameObject GameObject;
            public Button Button;
            public CanvasGroup CanvasGroup;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
        }

        private void Start()
        {
            Hide();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
            KillRevealSequence();
            ClearEntries();
        }

        public void Show(CardData[] options)
        {
            if (options == null || options.Length == 0)
            {
                return;
            }

            EnsureConfigured();
            _purchaseLocked = false;
            _options = options;

            _panelRoot.SetActive(true);
            _titleText.text = "Shop";
            _closeButtonText.text = "Close";

            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(OnCloseClicked);

            ClearEntries();

            var slots = new[] { _cardSlot0, _cardSlot1, _cardSlot2 };
            for (int i = 0; i < options.Length && i < slots.Length; i++)
            {
                var option = options[i];
                if (option == null)
                {
                    continue;
                }

                var go = Instantiate(_cardPrefab, slots[i]);
                var cardView = go.GetComponent<UICardView>();
                if (cardView == null)
                {
                    throw new InvalidOperationException("[UICardShopPanel] Shop card prefab must contain UICardView.");
                }

                var instance = new CardInstance(option);
                cardView.Setup(instance, CardViewContext.RewardPick);

                var button = go.GetComponent<Button>();
                if (button == null)
                {
                    throw new InvalidOperationException("[UICardShopPanel] Shop card prefab must contain Button.");
                }

                int capturedIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnOfferClicked(capturedIndex));

                var entry = new ShopEntry
                {
                    Index = i,
                    Card = option,
                    GameObject = go,
                    Button = button,
                    CanvasGroup = go.GetComponent<CanvasGroup>()
                };
                _entries.Add(entry);
                PrepareCardForReveal(go);
            }

            PlayRevealSequence();
            RefreshAffordableState();
        }

        public void Hide()
        {
            KillRevealSequence();
            _purchaseLocked = false;
            ClearEntries();
            _options = Array.Empty<CardData>();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        private void OnOfferClicked(int offerIndex)
        {
            if (_purchaseLocked || TurnManager.Instance == null)
            {
                return;
            }

            _purchaseLocked = true;
            bool purchased = TurnManager.Instance.TryPurchaseShopOffer(offerIndex);
            if (purchased)
            {
                RemovePurchasedEntry(offerIndex);
            }

            RefreshAffordableState();
            _purchaseLocked = false;
        }

        private void OnCloseClicked()
        {
            if (_purchaseLocked)
            {
                return;
            }

            TurnManager.Instance?.CloseCurrentShop();
        }

        private void OnMoneyChanged(GameEvents.MoneyChanged _)
        {
            RefreshAffordableState();
        }

        private void RefreshAffordableState()
        {
            int currentMoney = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry == null || entry.GameObject == null || entry.Card == null)
                {
                    continue;
                }

                bool affordable = currentMoney >= entry.Card.cost;
                entry.Button.interactable = affordable;
                if (entry.CanvasGroup != null)
                {
                    entry.CanvasGroup.alpha = affordable ? 1f : _disabledAlpha;
                }
            }
        }

        private void RemovePurchasedEntry(int offerIndex)
        {
            ShopEntry entry = null;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Index == offerIndex)
                {
                    entry = _entries[i];
                    _entries.RemoveAt(i);
                    break;
                }
            }

            if (entry == null || entry.GameObject == null)
            {
                return;
            }

            var cardRect = entry.GameObject.transform as RectTransform;
            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(entry.GameObject, LinkBehaviour.KillOnDestroy);
            if (entry.CanvasGroup != null)
            {
                sequence.Join(entry.CanvasGroup.DOFade(0f, _purchaseDuration).SetEase(Ease.OutQuad));
            }

            if (cardRect != null)
            {
                sequence.Join(cardRect.DOScale(Vector3.one * (_cardScale * 0.9f), _purchaseDuration).SetEase(Ease.OutQuad));
                sequence.Join(cardRect.DOAnchorPos(cardRect.anchoredPosition + new Vector2(0f, -10f), _purchaseDuration).SetEase(Ease.OutQuad));
            }

            sequence.OnComplete(() =>
            {
                if (entry.GameObject != null)
                {
                    Destroy(entry.GameObject);
                }
            });
        }

        private void PrepareCardForReveal(GameObject cardObject)
        {
            if (cardObject == null)
            {
                return;
            }

            var cardRect = cardObject.transform as RectTransform;
            var canvasGroup = cardObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (cardRect != null)
            {
                cardRect.localScale = Vector3.one * (_cardScale * 0.92f);
                cardRect.anchoredPosition += new Vector2(0f, -_revealYOffset);
            }

            cardObject.GetComponent<UICardHoverScale>()?.RefreshBaseline();
        }

        private void PlayRevealSequence()
        {
            KillRevealSequence();
            _revealSequence = DOTween.Sequence().SetUpdate(true);
            if (_panelRoot != null)
            {
                _revealSequence.SetLink(_panelRoot, LinkBehaviour.KillOnDestroy);
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry == null || entry.GameObject == null)
                {
                    continue;
                }

                var cardRect = entry.GameObject.transform as RectTransform;
                var canvasGroup = entry.CanvasGroup;
                var targetPosition = cardRect != null ? cardRect.anchoredPosition + new Vector2(0f, _revealYOffset) : Vector2.zero;

                if (i > 0)
                {
                    _revealSequence.AppendInterval(_revealStagger);
                }

                if (canvasGroup != null)
                {
                    _revealSequence.Join(canvasGroup.DOFade(1f, _revealDuration).SetEase(Ease.OutQuad).SetLink(entry.GameObject, LinkBehaviour.KillOnDestroy));
                }

                if (cardRect != null)
                {
                    _revealSequence.Join(cardRect.DOAnchorPos(targetPosition, _revealDuration).SetEase(Ease.OutBack).SetLink(entry.GameObject, LinkBehaviour.KillOnDestroy));
                    _revealSequence.Join(cardRect.DOScale(Vector3.one * _cardScale, _revealDuration).SetEase(Ease.OutBack).SetLink(entry.GameObject, LinkBehaviour.KillOnDestroy));
                }
            }
        }

        private void KillRevealSequence()
        {
            _revealSequence?.Kill(false);
            _revealSequence = null;
        }

        private void ClearEntries()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry?.GameObject != null)
                {
                    Destroy(entry.GameObject);
                }
            }

            _entries.Clear();
        }

        private void EnsureConfigured()
        {
            if (_panelRoot == null)
            {
                throw new InvalidOperationException("[UICardShopPanel] _panelRoot is not assigned in the Inspector.");
            }

            if (_titleText == null)
            {
                throw new InvalidOperationException("[UICardShopPanel] _titleText is not assigned in the Inspector.");
            }

            if (_cardSlot0 == null || _cardSlot1 == null || _cardSlot2 == null)
            {
                throw new InvalidOperationException("[UICardShopPanel] All three shop card slots must be explicitly assigned.");
            }

            if (_cardPrefab == null)
            {
                throw new InvalidOperationException("[UICardShopPanel] _cardPrefab is not assigned in the Inspector.");
            }

            if (_closeButton == null || _closeButtonText == null)
            {
                throw new InvalidOperationException("[UICardShopPanel] Close button references are not assigned in the Inspector.");
            }
        }
    }
}

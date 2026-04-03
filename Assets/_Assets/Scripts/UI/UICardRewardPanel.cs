using System;
using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Core;
using Martian.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 卡牌奖励选择面板，向玩家展示 3 张可选卡牌供选择或跳过。
    /// 在游戏流程中由 UIManager 驱动，向 GameEvents.CardRewardSelected 发布玩家选择。
    /// 支持普通和加强（Boosted）奖励的不同标题提示。
    /// </summary>
    public class UICardRewardPanel : MonoBehaviour
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

        [Header("Skip Button")]
        [SerializeField] private Button _skipButton;
        [SerializeField] private TextMeshProUGUI _skipButtonText;

        private CardData[] _options;
        private readonly List<GameObject> _spawnedCards = new();
        private bool _isBoosted;

        private void Start()
        {
            Hide();
        }

        public void Show(CardData[] options, bool boosted)
        {
            if (options == null || options.Length == 0)
            {
                return;
            }

            EnsureConfigured();

            _options = options;
            _isBoosted = boosted;

            _panelRoot.SetActive(true);
            ApplyText();

            _skipButton.onClick.RemoveAllListeners();
            _skipButton.onClick.AddListener(OnSkipClicked);

            ClearSpawnedCards();
            var slots = new[] { _cardSlot0, _cardSlot1, _cardSlot2 };

            for (int i = 0; i < options.Length && i < slots.Length; i++)
            {
                if (options[i] == null)
                {
                    throw new InvalidOperationException($"[UICardRewardPanel] Reward option at index {i} is null.");
                }

                var go = Instantiate(_cardPrefab, slots[i]);
                _spawnedCards.Add(go);

                var cardView = go.GetComponent<UICardView>();
                if (cardView == null)
                {
                    throw new InvalidOperationException("[UICardRewardPanel] Reward card prefab must contain UICardView.");
                }

                var instance = new CardInstance(options[i]);
                cardView.Setup(instance, CardViewContext.RewardPick);

                var button = go.GetComponent<Button>();
                if (button == null)
                {
                    throw new InvalidOperationException("[UICardRewardPanel] Reward card prefab must contain Button.");
                }

                int capturedIndex = i;
                button.interactable = true;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnCardClicked(capturedIndex));
            }
        }

        public void Hide()
        {
            ClearSpawnedCards();

            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveAllListeners();
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        private void OnCardClicked(int index)
        {
            if (_options == null || index < 0 || index >= _options.Length)
            {
                return;
            }

            var chosen = _options[index];
            Hide();
            EventBus.Publish(new GameEvents.CardRewardSelected { ChosenCard = chosen });
        }

        private void OnSkipClicked()
        {
            Hide();
            EventBus.Publish(new GameEvents.CardRewardSelected { ChosenCard = null });
        }

        private void ClearSpawnedCards()
        {
            foreach (var go in _spawnedCards)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            _spawnedCards.Clear();
        }

        private void ApplyText()
        {
            _titleText.text = _isBoosted ? GameText.RewardBoostedTitle : GameText.RewardTitle;
            _skipButtonText.text = GameText.RewardSkip;
        }

        private void EnsureConfigured()
        {
            if (_panelRoot == null)
            {
                throw new InvalidOperationException("[UICardRewardPanel] _panelRoot is not assigned in the Inspector.");
            }

            if (_titleText == null)
            {
                throw new InvalidOperationException("[UICardRewardPanel] _titleText is not assigned in the Inspector.");
            }

            if (_cardSlot0 == null || _cardSlot1 == null || _cardSlot2 == null)
            {
                throw new InvalidOperationException("[UICardRewardPanel] All three reward card slots must be explicitly assigned.");
            }

            if (_cardPrefab == null)
            {
                throw new InvalidOperationException("[UICardRewardPanel] _cardPrefab is not assigned in the Inspector.");
            }

            if (_skipButton == null)
            {
                throw new InvalidOperationException("[UICardRewardPanel] _skipButton is not assigned in the Inspector.");
            }

            if (_skipButtonText == null)
            {
                throw new InvalidOperationException("[UICardRewardPanel] _skipButtonText is not assigned in the Inspector.");
            }
        }
    }
}

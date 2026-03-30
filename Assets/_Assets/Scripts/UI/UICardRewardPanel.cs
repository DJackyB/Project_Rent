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
    /// 三选一卡牌奖励面板。
    /// 结算动画播完后由 UIManager 调用 Show()，玩家点击卡牌或跳过后发布 CardRewardSelected 事件。
    /// 所有 UI 元素在 Editor 中拼接，脚本只通过 SerializeField 引用。
    /// </summary>
    public class UICardRewardPanel : MonoBehaviour
    {
        [Header("面板根节点")]
        [SerializeField] private GameObject _panelRoot;

        [Header("标题")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Header("卡牌槽位（Inspector 拖入 3 个 Transform 容器）")]
        [SerializeField] private Transform _cardSlot0;
        [SerializeField] private Transform _cardSlot1;
        [SerializeField] private Transform _cardSlot2;

        [Header("卡牌 Prefab（与 UIHandPanel 相同）")]
        [SerializeField] private GameObject _cardPrefab;

        [Header("跳过按钮")]
        [SerializeField] private Button _skipButton;
        [SerializeField] private TextMeshProUGUI _skipButtonText;

        private CardData[] _options;
        private readonly List<GameObject> _spawnedCards = new();
        private bool _isShowing;
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
            _isShowing = true;

            _panelRoot.SetActive(true);
            ApplyLocalizedTexts();

            _skipButton.onClick.RemoveAllListeners();
            _skipButton.onClick.AddListener(OnSkipClicked);

            // 在 3 个槽位中生成卡牌
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

                // 用临时 CardInstance 驱动显示
                var instance = new CardInstance(options[i]);
                cardView.Setup(instance, CardViewContext.RewardPick);

                // 启用按钮点击，绑定选择回调
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
            _isShowing = false;
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

        public void RefreshLocalization()
        {
            if (!_isShowing || _options == null)
            {
                return;
            }

            ApplyLocalizedTexts();

            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                var cardView = _spawnedCards[i] != null ? _spawnedCards[i].GetComponent<UICardView>() : null;
                if (cardView != null && cardView.Card != null)
                {
                    cardView.Setup(cardView.Card, CardViewContext.RewardPick);
                }
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

        private void ApplyLocalizedTexts()
        {
            _titleText.text = _isBoosted ? UIStrings.RewardBoostedTitle : UIStrings.RewardTitle;
            UIFontCatalog.ApplyToText(_titleText);

            _skipButtonText.text = UIStrings.RewardSkip;
            UIFontCatalog.ApplyToText(_skipButtonText);
        }

        private void EnsureConfigured()
        {
            if (_panelRoot == null)
            {
                throw new InvalidOperationException("[UICardRewardPanel] _panelRoot 未在 Inspector 中赋值。");
            }

            if (_titleText == null)
            {
                throw new InvalidOperationException("[UICardRewardPanel] _titleText 未在 Inspector 中赋值。");
            }

            if (_cardSlot0 == null || _cardSlot1 == null || _cardSlot2 == null)
            {
                throw new InvalidOperationException("[UICardRewardPanel] 3 个卡牌槽位必须全部显式配置。");
            }

            if (_cardPrefab == null)
            {
                throw new InvalidOperationException("[UICardRewardPanel] _cardPrefab 未在 Inspector 中赋值。");
            }

            if (_skipButton == null)
            {
                throw new InvalidOperationException("[UICardRewardPanel] _skipButton 未在 Inspector 中赋值。");
            }

            if (_skipButtonText == null)
            {
                throw new InvalidOperationException("[UICardRewardPanel] _skipButtonText 未在 Inspector 中赋值。");
            }
        }
    }
}

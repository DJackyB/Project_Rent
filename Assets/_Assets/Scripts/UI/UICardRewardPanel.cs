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

            _options = options;
            _isShowing = true;

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }

            // 标题
            if (_titleText != null)
            {
                _titleText.text = boosted ? UIStrings.RewardBoostedTitle : UIStrings.RewardTitle;
                UIFontCatalog.ApplyToText(_titleText);
            }

            // 跳过按钮
            if (_skipButtonText != null)
            {
                _skipButtonText.text = UIStrings.RewardSkip;
                UIFontCatalog.ApplyToText(_skipButtonText);
            }

            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveAllListeners();
                _skipButton.onClick.AddListener(OnSkipClicked);
            }

            // 在 3 个槽位中生成卡牌
            ClearSpawnedCards();
            var slots = new[] { _cardSlot0, _cardSlot1, _cardSlot2 };

            for (int i = 0; i < options.Length && i < slots.Length; i++)
            {
                if (slots[i] == null || _cardPrefab == null || options[i] == null)
                {
                    continue;
                }

                var go = Instantiate(_cardPrefab, slots[i]);
                _spawnedCards.Add(go);

                var cardView = go.GetComponent<UICardView>();
                if (cardView == null)
                {
                    continue;
                }

                // 用临时 CardInstance 驱动显示
                var instance = new CardInstance(options[i]);
                cardView.Setup(instance, CardViewContext.RewardPick);

                // 启用按钮点击，绑定选择回调
                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    int capturedIndex = i;
                    button.interactable = true;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnCardClicked(capturedIndex));
                }
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

            if (_titleText != null)
            {
                UIFontCatalog.ApplyToText(_titleText);
            }

            if (_skipButtonText != null)
            {
                _skipButtonText.text = UIStrings.RewardSkip;
                UIFontCatalog.ApplyToText(_skipButtonText);
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
    }
}

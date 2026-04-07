using System;
using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Integration.Feel;
using DG.Tweening;
using Martian.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// Reward selection panel that reveals up to three candidate cards and publishes the chosen result.
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

        [Header("Optional Feedback")]
        [SerializeField] private BaoZuPoFeelFeedbackInstaller _feelFeedbackInstaller;

        [Header("Reward Motion")]
        [SerializeField] private float _revealDuration = 0.2f;
        [SerializeField] private float _revealStagger = 0.06f;
        [SerializeField] private float _revealYOffset = 24f;
        [SerializeField] private float _selectedDuration = 0.16f;
        [SerializeField] private float _nonSelectedFade = 0.18f;

        private CardData[] _options;
        private readonly List<GameObject> _spawnedCards = new();
        private bool _isBoosted;
        private bool _selectionLocked;
        private Sequence _revealSequence;
        private Sequence _selectionSequence;

        private void Start()
        {
            Hide();
        }

        private void OnDisable()
        {
            KillSequences();
            KillSpawnedCardTweens();
            ClearSpawnedCards();
        }

        public void Show(CardData[] options, bool boosted)
        {
            if (options == null || options.Length == 0)
            {
                return;
            }

            EnsureConfigured();
            KillSequences();
            _selectionLocked = false;

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

                PrepareCardForReveal(go);
            }

            PlayRevealSequence();
            PlayRewardFeelFeedback(_panelRoot.transform.position, "RewardReveal");
        }

        public void Hide()
        {
            KillSequences();
            _selectionLocked = false;
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
            if (_selectionLocked || _options == null || index < 0 || index >= _options.Length)
            {
                return;
            }

            _selectionLocked = true;
            var chosen = _options[index];
            var feedbackPosition = ResolveRewardCardPosition(index);
            PlayRewardFeelFeedback(feedbackPosition, "RewardSelected");
            PlayRewardSelectionSequence(index, () =>
            {
                Hide();
                EventBus.Publish(new GameEvents.CardRewardSelected
                {
                    ChosenCard = chosen,
                    HasSourceWorldPosition = true,
                    SourceWorldPosition = feedbackPosition
                });
            });
        }

        private void OnSkipClicked()
        {
            if (_selectionLocked)
            {
                return;
            }

            Hide();
            EventBus.Publish(new GameEvents.CardRewardSelected
            {
                ChosenCard = null,
                HasSourceWorldPosition = false,
                SourceWorldPosition = Vector3.zero
            });
        }

        private Vector3 ResolveRewardCardPosition(int index)
        {
            if (index >= 0 && index < _spawnedCards.Count && _spawnedCards[index] != null)
            {
                return _spawnedCards[index].transform.position;
            }

            return _panelRoot != null ? _panelRoot.transform.position : transform.position;
        }

        private void PlayRewardFeelFeedback(Vector3 position, string debugLabel)
        {
            if (_feelFeedbackInstaller == null || _feelFeedbackInstaller.FeelBackend == null)
            {
                return;
            }

            _feelFeedbackInstaller.FeelBackend.PlaySlotAt(FeelFeedbackSlots.RewardReveal, position, debugLabel);
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
                cardRect.localScale = Vector3.one * 0.92f;
                cardRect.anchoredPosition += new Vector2(0f, -_revealYOffset);
            }
        }

        private void PlayRevealSequence()
        {
            KillRevealSequence();
            _revealSequence = DOTween.Sequence().SetUpdate(true);
            if (_panelRoot != null)
            {
                _revealSequence.SetLink(_panelRoot, LinkBehaviour.KillOnDestroy);
            }

            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                var cardObject = _spawnedCards[i];
                if (cardObject == null)
                {
                    continue;
                }

                var cardRect = cardObject.transform as RectTransform;
                var canvasGroup = cardObject.GetComponent<CanvasGroup>();
                var targetPosition = cardRect != null ? cardRect.anchoredPosition + new Vector2(0f, _revealYOffset) : Vector2.zero;

                if (i > 0)
                {
                    _revealSequence.AppendInterval(_revealStagger);
                }

                if (canvasGroup != null)
                {
                    _revealSequence.Join(
                        canvasGroup.DOFade(1f, _revealDuration)
                            .SetEase(Ease.OutQuad)
                            .SetLink(cardObject, LinkBehaviour.KillOnDestroy));
                }

                if (cardRect != null)
                {
                    _revealSequence.Join(
                        cardRect.DOAnchorPos(targetPosition, _revealDuration)
                            .SetEase(Ease.OutBack)
                            .SetLink(cardObject, LinkBehaviour.KillOnDestroy));
                    _revealSequence.Join(
                        cardRect.DOScale(Vector3.one, _revealDuration)
                            .SetEase(Ease.OutBack)
                            .SetLink(cardObject, LinkBehaviour.KillOnDestroy));
                }
            }
        }

        private void PlayRewardSelectionSequence(int selectedIndex, Action onComplete)
        {
            KillSelectionSequence();
            _selectionSequence = DOTween.Sequence().SetUpdate(true);
            if (_panelRoot != null)
            {
                _selectionSequence.SetLink(_panelRoot, LinkBehaviour.KillOnDestroy);
            }

            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                var cardObject = _spawnedCards[i];
                if (cardObject == null)
                {
                    continue;
                }

                bool isSelected = i == selectedIndex;
                var cardRect = cardObject.transform as RectTransform;
                var canvasGroup = cardObject.GetComponent<CanvasGroup>();
                var cardView = cardObject.GetComponent<UICardView>();

                if (cardView != null)
                {
                    cardView.SetSelected(isSelected);
                    if (isSelected)
                    {
                        cardView.PlayCommitInteractionFeedback();
                    }
                }

                if (cardRect != null)
                {
                    _selectionSequence.Join(cardRect.DOScale(Vector3.one * (isSelected ? 1.08f : 0.94f), _selectedDuration)
                        .SetEase(isSelected ? Ease.OutBack : Ease.OutQuad)
                        .SetLink(cardObject, LinkBehaviour.KillOnDestroy));
                    if (!isSelected)
                    {
                        _selectionSequence.Join(cardRect.DOAnchorPos(cardRect.anchoredPosition + new Vector2(0f, -10f), _selectedDuration)
                            .SetEase(Ease.OutQuad)
                            .SetLink(cardObject, LinkBehaviour.KillOnDestroy));
                    }
                }

                if (canvasGroup != null)
                {
                    _selectionSequence.Join(canvasGroup.DOFade(isSelected ? 1f : _nonSelectedFade, _selectedDuration)
                        .SetEase(Ease.OutQuad)
                        .SetLink(cardObject, LinkBehaviour.KillOnDestroy));
                }
            }

            _selectionSequence.OnComplete(() => onComplete?.Invoke());
        }

        private void KillSequences()
        {
            KillRevealSequence();
            KillSelectionSequence();
        }

        private void KillRevealSequence()
        {
            _revealSequence?.Kill(false);
            _revealSequence = null;
        }

        private void KillSelectionSequence()
        {
            _selectionSequence?.Kill(false);
            _selectionSequence = null;
        }

        private void ClearSpawnedCards()
        {
            foreach (var go in _spawnedCards)
            {
                if (go != null)
                {
                    KillCardTweens(go);
                    Destroy(go);
                }
            }

            _spawnedCards.Clear();
        }

        private void KillSpawnedCardTweens()
        {
            foreach (var go in _spawnedCards)
            {
                KillCardTweens(go);
            }
        }

        private static void KillCardTweens(GameObject cardObject)
        {
            if (cardObject == null)
            {
                return;
            }

            foreach (var rect in cardObject.GetComponentsInChildren<RectTransform>(true))
            {
                rect.DOKill(false);
            }

            foreach (var graphic in cardObject.GetComponentsInChildren<Graphic>(true))
            {
                graphic.DOKill(false);
            }

            foreach (var canvasGroup in cardObject.GetComponentsInChildren<CanvasGroup>(true))
            {
                canvasGroup.DOKill(false);
            }
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

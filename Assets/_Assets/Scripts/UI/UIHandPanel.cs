using System.Collections;
using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Deck;
using BaoZuPo.UI.Common.Drag;
using DG.Tweening;
using Martian.Tooltip;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    public enum UIHandIncomingAnimationKind
    {
        FirstTurnDraw,
        TurnDraw,
        RewardPick,
        Generated
    }

    /// <summary>
    /// Hand panel that owns hand card views and lightweight incoming-card presentation.
    /// </summary>
    public class UIHandPanel : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("Hand card prefab. Must contain UICardView.")]
        public GameObject cardPrefab;

        [Tooltip("Hand container. This should be assigned explicitly.")]
        public Transform handContainer;

        [Header("Incoming Card Animation")]
        [SerializeField] private float drawMoveSeconds = 0.2f;
        [SerializeField] private float firstTurnMoveSeconds = 0.24f;
        [SerializeField] private float generatedMoveSeconds = 0.18f;
        [SerializeField] private float rewardMoveSeconds = 0.28f;
        [SerializeField] private float revealSeconds = 0.12f;
        [SerializeField] private float betweenCardsPauseSeconds = 0.06f;
        [SerializeField] private float drawStartYOffset = 150f;
        [SerializeField] private float firstTurnStartYOffset = 190f;
        [SerializeField] private float generatedStartYOffset = 92f;
        [SerializeField] private float rewardStartScale = 0.88f;
        [SerializeField] private float drawStartScale = 0.8f;
        [SerializeField] private float firstTurnStartScale = 0.76f;
        [SerializeField] private float generatedStartScale = 0.9f;
        [SerializeField] private float targetStartScale = 0.9f;
        [SerializeField] private float targetPunchScale = 1.04f;

        private readonly List<UICardView> _cardViews = new();
        private readonly List<UICardView> _orderedViewsBuffer = new();
        private Canvas _rootCanvas;
        private RectTransform _animationLayerRoot;
        private bool _runtimeAnimationLayerReady;

        public float BetweenCardsPauseSeconds => betweenCardsPauseSeconds;

        public void RefreshHand()
        {
            if (!TryResolveHandContext(out var container, out var hand))
            {
                return;
            }

            SyncHandViews(container, hand);
            RefreshHandLayout(container as RectTransform, true);
        }

        public IEnumerator PlayIncomingCard(CardInstance card, UIHandIncomingAnimationKind kind, Vector3? sourceWorldPosition = null)
        {
            if (card == null)
            {
                yield break;
            }

            var targetView = EnsureCardViewForAppend(card);
            if (targetView == null)
            {
                yield break;
            }

            EnsureAnimationLayer();
            if (_animationLayerRoot == null)
            {
                yield break;
            }

            RefreshHandLayout(handContainer as RectTransform, true);
            var targetRect = targetView.transform as RectTransform;
            if (targetRect == null)
            {
                yield break;
            }

            var targetCanvasGroup = EnsureCanvasGroup(targetView.gameObject);
            if (targetCanvasGroup != null)
            {
                targetCanvasGroup.alpha = 0f;
            }

            targetRect.localScale = Vector3.one * targetStartScale;

            var ghostObject = Instantiate(cardPrefab, _animationLayerRoot);
            ghostObject.name = $"{card.Data.cardName}_IncomingGhost";
            var ghostView = ghostObject.GetComponent<UICardView>();
            if (ghostView == null)
            {
                Destroy(ghostObject);
                if (targetCanvasGroup != null)
                {
                    targetCanvasGroup.alpha = 1f;
                }

                targetRect.localScale = Vector3.one;
                yield break;
            }

            ghostView.Setup(card, CardViewContext.Hand, this);
            DisableGhostInteraction(ghostObject);

            var ghostRect = ghostObject.transform as RectTransform;
            var ghostCanvasGroup = EnsureCanvasGroup(ghostObject);
            Vector3 startPosition = ResolveIncomingStartPosition(kind, targetRect, sourceWorldPosition);
            float moveSeconds = ResolveMoveSeconds(kind);
            float startScale = ResolveStartScale(kind);

            ghostRect.position = startPosition;
            ghostRect.localScale = Vector3.one * startScale;
            if (ghostCanvasGroup != null)
            {
                ghostCanvasGroup.alpha = 0.95f;
            }

            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(ghostObject, LinkBehaviour.KillOnDestroy);
            sequence.Join(ghostRect.DOMove(targetRect.position, moveSeconds).SetEase(Ease.OutCubic).SetLink(ghostObject, LinkBehaviour.KillOnDestroy));
            sequence.Join(ghostRect.DOScale(Vector3.one, moveSeconds).SetEase(Ease.OutBack).SetLink(ghostObject, LinkBehaviour.KillOnDestroy));

            if (targetCanvasGroup != null)
            {
                sequence.Join(targetCanvasGroup.DOFade(1f, revealSeconds).SetDelay(moveSeconds * 0.55f).SetEase(Ease.OutQuad).SetLink(targetView.gameObject, LinkBehaviour.KillOnDestroy));
            }

            sequence.Join(targetRect.DOScale(Vector3.one * targetPunchScale, revealSeconds).SetDelay(moveSeconds * 0.55f).SetEase(Ease.OutQuad).SetLink(targetView.gameObject, LinkBehaviour.KillOnDestroy));

            if (ghostCanvasGroup != null)
            {
                sequence.Append(ghostCanvasGroup.DOFade(0f, revealSeconds * 0.8f).SetEase(Ease.InQuad).SetLink(ghostObject, LinkBehaviour.KillOnDestroy));
            }

            sequence.Join(targetRect.DOScale(Vector3.one, revealSeconds).SetEase(Ease.OutBack).SetLink(targetView.gameObject, LinkBehaviour.KillOnDestroy));

            yield return sequence.WaitForCompletion();

            if (ghostObject != null)
            {
                Destroy(ghostObject);
            }

            if (targetCanvasGroup != null)
            {
                targetCanvasGroup.alpha = 1f;
            }

            targetRect.localScale = Vector3.one;

            if (betweenCardsPauseSeconds > 0f)
            {
                yield return new WaitForSeconds(betweenCardsPauseSeconds);
            }
        }

        public UICardView FindView(CardInstance card)
        {
            if (card == null)
            {
                return null;
            }

            for (int i = 0; i < _cardViews.Count; i++)
            {
                if (_cardViews[i] != null && ReferenceEquals(_cardViews[i].Card, card))
                {
                    return _cardViews[i];
                }
            }

            return null;
        }

        private UICardView EnsureCardViewForAppend(CardInstance card)
        {
            if (card == null)
            {
                return null;
            }

            if (!TryResolveHandContext(out var container, out var hand))
            {
                return null;
            }

            if (!ContainsCard(hand, card))
            {
                return null;
            }

            SyncHandViews(container, hand);
            RefreshHandLayout(container as RectTransform, true);
            return FindView(card);
        }

        private UICardView CreateCardView(Transform parent, CardInstance card)
        {
            var cardObject = Instantiate(cardPrefab, parent);
            var cardView = cardObject.GetComponent<UICardView>();
            if (cardView == null)
            {
                Debug.LogError("[UIHandPanel] Card prefab requires UICardView.");
                DestroyCardObjectImmediately(cardObject);
                return null;
            }

            cardView.Setup(card, CardViewContext.Hand, this);
            return cardView;
        }

        private bool TryResolveHandContext(out Transform container, out IReadOnlyList<CardInstance> hand)
        {
            container = EnsureContainer();
            hand = null;

            if (container == null)
            {
                return false;
            }

            if (cardPrefab == null)
            {
                Debug.LogError("[UIHandPanel] Missing cardPrefab.");
                return false;
            }

            if (DeckManager.Instance == null)
            {
                return false;
            }

            hand = DeckManager.Instance.Hand;
            return true;
        }

        private Transform EnsureContainer()
        {
            if (handContainer == null)
            {
                Debug.LogError("[UIHandPanel] handContainer is not assigned in the Inspector. Create a HandContainer child under UIHandPanel and assign it.");
            }

            return handContainer;
        }

        private void EnsureAnimationLayer()
        {
            if (_runtimeAnimationLayerReady && _animationLayerRoot != null)
            {
                return;
            }

            if (_rootCanvas == null)
            {
                _rootCanvas = GetComponentInParent<Canvas>();
            }

            if (_rootCanvas == null)
            {
                return;
            }

            if (_animationLayerRoot == null)
            {
                var layerObject = new GameObject("HandIncomingAnimationLayer", typeof(RectTransform), typeof(CanvasGroup));
                layerObject.transform.SetParent(_rootCanvas.transform, false);
                _animationLayerRoot = layerObject.GetComponent<RectTransform>();
                _animationLayerRoot.anchorMin = Vector2.zero;
                _animationLayerRoot.anchorMax = Vector2.one;
                _animationLayerRoot.offsetMin = Vector2.zero;
                _animationLayerRoot.offsetMax = Vector2.zero;
                _animationLayerRoot.pivot = new Vector2(0.5f, 0.5f);

                var canvasGroup = layerObject.GetComponent<CanvasGroup>();
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            _runtimeAnimationLayerReady = true;
        }

        private Vector3 ResolveIncomingStartPosition(UIHandIncomingAnimationKind kind, RectTransform targetRect, Vector3? sourceWorldPosition)
        {
            if (kind == UIHandIncomingAnimationKind.RewardPick && sourceWorldPosition.HasValue)
            {
                return sourceWorldPosition.Value;
            }

            Vector3 fallbackTarget = targetRect != null ? targetRect.position : handContainer != null ? handContainer.position : transform.position;
            float verticalOffset = kind switch
            {
                UIHandIncomingAnimationKind.FirstTurnDraw => firstTurnStartYOffset,
                UIHandIncomingAnimationKind.Generated => generatedStartYOffset,
                UIHandIncomingAnimationKind.RewardPick => drawStartYOffset * 0.6f,
                _ => drawStartYOffset
            };

            float horizontalOffset = kind switch
            {
                UIHandIncomingAnimationKind.FirstTurnDraw => -18f,
                UIHandIncomingAnimationKind.Generated => 26f,
                _ => 0f
            };

            return fallbackTarget + new Vector3(horizontalOffset, verticalOffset, 0f);
        }

        private float ResolveMoveSeconds(UIHandIncomingAnimationKind kind)
        {
            return kind switch
            {
                UIHandIncomingAnimationKind.FirstTurnDraw => firstTurnMoveSeconds,
                UIHandIncomingAnimationKind.Generated => generatedMoveSeconds,
                UIHandIncomingAnimationKind.RewardPick => rewardMoveSeconds,
                _ => drawMoveSeconds
            };
        }

        private float ResolveStartScale(UIHandIncomingAnimationKind kind)
        {
            return kind switch
            {
                UIHandIncomingAnimationKind.FirstTurnDraw => firstTurnStartScale,
                UIHandIncomingAnimationKind.Generated => generatedStartScale,
                UIHandIncomingAnimationKind.RewardPick => rewardStartScale,
                _ => drawStartScale
            };
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.AddComponent<CanvasGroup>();
            }

            return canvasGroup;
        }

        private static void DisableGhostInteraction(GameObject ghostObject)
        {
            if (ghostObject == null)
            {
                return;
            }

            var canvasGroup = EnsureCanvasGroup(ghostObject);
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            foreach (var graphic in ghostObject.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

            foreach (var button in ghostObject.GetComponentsInChildren<Button>(true))
            {
                button.interactable = false;
                button.enabled = false;
            }

            foreach (var layoutElement in ghostObject.GetComponentsInChildren<LayoutElement>(true))
            {
                layoutElement.ignoreLayout = true;
                layoutElement.enabled = false;
            }

            foreach (var dragHandler in ghostObject.GetComponentsInChildren<UICardDragHandler>(true))
            {
                dragHandler.enabled = false;
            }

            foreach (var hoverScale in ghostObject.GetComponentsInChildren<UICardHoverScale>(true))
            {
                hoverScale.enabled = false;
            }

            foreach (var tooltipTrigger in ghostObject.GetComponentsInChildren<TooltipTrigger>(true))
            {
                tooltipTrigger.enabled = false;
            }
        }

        private void SyncHandViews(Transform container, IReadOnlyList<CardInstance> hand)
        {
            RemoveViewsMissingFromHand(hand);
            RemoveUntrackedChildren(container);

            _orderedViewsBuffer.Clear();
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                var cardView = FindView(card);
                if (cardView == null)
                {
                    cardView = CreateCardView(container, card);
                }

                if (cardView == null)
                {
                    continue;
                }

                if (cardView.transform.parent != container)
                {
                    cardView.transform.SetParent(container, false);
                }

                cardView.transform.SetSiblingIndex(_orderedViewsBuffer.Count);
                _orderedViewsBuffer.Add(cardView);
            }

            _cardViews.Clear();
            _cardViews.AddRange(_orderedViewsBuffer);
        }

        private void RemoveViewsMissingFromHand(IReadOnlyList<CardInstance> hand)
        {
            for (int i = _cardViews.Count - 1; i >= 0; i--)
            {
                var view = _cardViews[i];
                if (view == null)
                {
                    _cardViews.RemoveAt(i);
                    continue;
                }

                if (ContainsCard(hand, view.Card))
                {
                    continue;
                }

                DisposeView(view);
                _cardViews.RemoveAt(i);
            }
        }

        private void RemoveUntrackedChildren(Transform container)
        {
            if (container == null)
            {
                return;
            }

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i);
                var view = child.GetComponent<UICardView>();
                if (view != null && _cardViews.Contains(view))
                {
                    continue;
                }

                DisposeCardObject(child.gameObject);
            }
        }

        private void RefreshHandLayout(RectTransform containerRect, bool snapToIdle)
        {
            if (containerRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

            for (int i = 0; i < _cardViews.Count; i++)
            {
                if (_cardViews[i] != null)
                {
                    _cardViews[i].RefreshDragLayoutBaseline(snapToIdle);
                }
            }
        }

        private static bool ContainsCard(IReadOnlyList<CardInstance> hand, CardInstance card)
        {
            if (hand == null || card == null)
            {
                return false;
            }

            for (int i = 0; i < hand.Count; i++)
            {
                if (ReferenceEquals(hand[i], card))
                {
                    return true;
                }
            }

            return false;
        }

        private static void DisposeView(UICardView view)
        {
            if (view == null)
            {
                return;
            }

            DisposeCardObject(view.gameObject);
        }

        private static void DisposeCardObject(GameObject cardObject)
        {
            if (cardObject == null)
            {
                return;
            }

            foreach (var rect in cardObject.GetComponentsInChildren<RectTransform>(true))
            {
                rect.DOKill(false);
            }

            foreach (var canvasGroup in cardObject.GetComponentsInChildren<CanvasGroup>(true))
            {
                canvasGroup.DOKill(false);
            }

            // Detach before destroying so stale cards do not affect same-frame layout.
            cardObject.SetActive(false);
            cardObject.transform.SetParent(null, false);
            DestroyCardObjectImmediately(cardObject);
        }

        private static void DestroyCardObjectImmediately(GameObject cardObject)
        {
            if (cardObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(cardObject);
                return;
            }

            DestroyImmediate(cardObject);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using BaoZuPo.Integration.Martian.Feedback;
using Martian.Feedback;
using Martian.Feedback.Runtime;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace BaoZuPo.Integration.Feel
{
    /// <summary>
    /// Optional Feel visual feedback backend. It maps Martian.Feedback requests to explicitly registered MMF_Players.
    /// With no registered players, it is intentionally inert.
    /// </summary>
    public sealed class FeelFeedbackBackend : IFeedbackPlaybackBackend
    {
        private readonly Dictionary<string, MMF_Player> _players = new();
        private bool _attached;
        private Transform _host;
        private Canvas _hostCanvas;

        public bool IsAvailable => _attached && _players.Count > 0;

        public event Action AllPlaybackCompleted
        {
            add { }
            remove { }
        }

        public void RegisterPlayer(string slot, MMF_Player player)
        {
            if (string.IsNullOrEmpty(slot)) return;

            if (player != null)
            {
                _players[slot] = player;
            }
            else
            {
                _players.Remove(slot);
            }
        }

        public void Attach(Transform host)
        {
            _attached = host != null;
            _host = host;
            _hostCanvas = host != null ? host.GetComponentInParent<Canvas>() : null;
        }

        public void Configure(FeedbackRuntimeOptions options)
        {
        }

        public FeedbackPlaybackHandle Publish(FeedbackRequest request)
        {
            if (_players.Count == 0)
            {
                return null;
            }

            if (request != null)
            {
                string slot = ResolveSlot(request.Category);
                if (TryResolvePosition(request.Anchor, request.ScreenOffset, request.UseScreenCenterFallback, out var position))
                {
                    PlaySlotAt(slot, position);
                }
                else
                {
                    PlaySlot(slot);
                }
            }

            return null;
        }

        public FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request)
        {
            if (_players.Count == 0)
            {
                return null;
            }

            if (request != null && request.Steps != null && request.Steps.Count > 0)
            {
                FeedbackStep first = request.Steps[0];
                string slot = ResolveSlot(first.Category);
                Vector2 offset = request.ScreenOffset + first.Offset;
                if (TryResolvePosition(request.Anchor, offset, request.UseScreenCenterFallback, out var position))
                {
                    PlaySlotAt(slot, position);
                }
                else
                {
                    PlaySlot(slot);
                }
            }

            return null;
        }

        public void Clear()
        {
            foreach (var kvp in _players)
            {
                if (kvp.Value != null && kvp.Value.IsPlaying)
                {
                    kvp.Value.StopFeedbacks();
                }
            }
        }

        public void PlaySlot(string slot, string debugLabel = null)
        {
            if (string.IsNullOrEmpty(slot)) return;

            if (!_players.TryGetValue(slot, out MMF_Player player) || player == null)
            {
                return;
            }

            player.PlayFeedbacks();
        }

        public void PlaySlotAt(string slot, Vector3 position, string debugLabel = null)
        {
            if (string.IsNullOrEmpty(slot)) return;

            if (!_players.TryGetValue(slot, out MMF_Player player) || player == null)
            {
                return;
            }

            player.transform.position = position;
            player.PlayFeedbacks(position);
        }

        public float PlaySlotAttached(string slot, RectTransform anchor, string debugLabel = null)
        {
            if (string.IsNullOrEmpty(slot) || anchor == null)
            {
                return 0f;
            }

            if (!_players.TryGetValue(slot, out MMF_Player player) || player == null)
            {
                return 0f;
            }

            Transform playerTransform = player.transform;
            Transform originalParent = playerTransform.parent;
            int originalSiblingIndex = playerTransform.GetSiblingIndex();

            Vector2 originalAnchorMin = Vector2.zero;
            Vector2 originalAnchorMax = Vector2.zero;
            Vector2 originalPivot = new Vector2(0.5f, 0.5f);
            Vector2 originalSizeDelta = Vector2.zero;
            Vector2 originalAnchoredPosition = Vector2.zero;
            Vector3 originalLocalScale = Vector3.one;

            var playerRect = playerTransform as RectTransform;
            if (playerRect != null)
            {
                originalAnchorMin = playerRect.anchorMin;
                originalAnchorMax = playerRect.anchorMax;
                originalPivot = playerRect.pivot;
                originalSizeDelta = playerRect.sizeDelta;
                originalAnchoredPosition = playerRect.anchoredPosition;
                originalLocalScale = playerRect.localScale;

                playerRect.SetParent(anchor, false);
                playerRect.SetAsFirstSibling();
                playerRect.anchorMin = new Vector2(0.5f, 0.5f);
                playerRect.anchorMax = new Vector2(0.5f, 0.5f);
                playerRect.pivot = new Vector2(0.5f, 0.5f);
                playerRect.anchoredPosition = Vector2.zero;
                playerRect.sizeDelta = anchor.rect.size;
                playerRect.localScale = Vector3.one;
            }
            else
            {
                playerTransform.SetParent(anchor, false);
                playerTransform.SetAsFirstSibling();
                playerTransform.localPosition = Vector3.zero;
                playerTransform.localScale = Vector3.one;
            }

            player.PlayFeedbacks(anchor.position);

            float duration = Mathf.Max(0f, player.TotalDuration);
            player.StartCoroutine(RestorePlayerAfterDelay(
                player,
                originalParent,
                originalSiblingIndex,
                playerRect,
                originalAnchorMin,
                originalAnchorMax,
                originalPivot,
                originalSizeDelta,
                originalAnchoredPosition,
                originalLocalScale,
                duration));

            return duration;
        }

        private static string ResolveSlot(string category)
        {
            return category switch
            {
                BaoZuPoFeedbackCategories.Money => FeelFeedbackSlots.MoneyDelta,
                BaoZuPoFeedbackCategories.Cost => FeelFeedbackSlots.MoneyDelta,
                BaoZuPoFeedbackCategories.Settlement => FeelFeedbackSlots.SettlementStep,
                BaoZuPoFeedbackCategories.Loan => FeelFeedbackSlots.LoanPayment,
                _ => null
            };
        }

        private bool TryResolvePosition(RectTransform anchor, Vector2 screenOffset, bool useScreenCenterFallback, out Vector3 position)
        {
            position = default;

            if (!_attached)
            {
                return false;
            }

            Camera camera = ResolveCamera();
            Vector2 screenPosition;
            if (anchor != null && anchor.gameObject.activeInHierarchy)
            {
                Vector3 anchorWorldPosition = anchor.TransformPoint(anchor.rect.center);
                screenPosition = RectTransformUtility.WorldToScreenPoint(camera, anchorWorldPosition) + screenOffset;
            }
            else
            {
                if (!useScreenCenterFallback)
                {
                    return false;
                }

                screenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + screenOffset;
            }

            RectTransform canvasRect = _hostCanvas != null ? _hostCanvas.transform as RectTransform : null;
            if (canvasRect != null &&
                RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPosition, camera, out var worldPosition))
            {
                position = worldPosition;
                return true;
            }

            if (_host != null)
            {
                position = _host.position + new Vector3(screenOffset.x, screenOffset.y, 0f);
                return true;
            }

            return false;
        }

        private Camera ResolveCamera()
        {
            if (_hostCanvas == null || _hostCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return _hostCanvas.worldCamera != null ? _hostCanvas.worldCamera : Camera.main;
        }

        private static IEnumerator RestorePlayerAfterDelay(
            MMF_Player player,
            Transform originalParent,
            int originalSiblingIndex,
            RectTransform playerRect,
            Vector2 originalAnchorMin,
            Vector2 originalAnchorMax,
            Vector2 originalPivot,
            Vector2 originalSizeDelta,
            Vector2 originalAnchoredPosition,
            Vector3 originalLocalScale,
            float duration)
        {
            if (duration > 0f)
            {
                yield return new WaitForSecondsRealtime(duration);
            }

            if (player == null || originalParent == null)
            {
                yield break;
            }

            if (playerRect != null)
            {
                playerRect.SetParent(originalParent, false);
                playerRect.SetSiblingIndex(Mathf.Min(originalSiblingIndex, originalParent.childCount - 1));
                playerRect.anchorMin = originalAnchorMin;
                playerRect.anchorMax = originalAnchorMax;
                playerRect.pivot = originalPivot;
                playerRect.sizeDelta = originalSizeDelta;
                playerRect.anchoredPosition = originalAnchoredPosition;
                playerRect.localScale = originalLocalScale;
            }
            else
            {
                player.transform.SetParent(originalParent, false);
                player.transform.SetSiblingIndex(Mathf.Min(originalSiblingIndex, originalParent.childCount - 1));
                player.transform.localScale = originalLocalScale;
            }
        }
    }
}

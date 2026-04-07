using System;
using System.Collections.Generic;
using BaoZuPo.Integration.Martian.Feedback;
using Martian.Feedback;
using Martian.Feedback.Runtime;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace BaoZuPo.Integration.Feel
{
    /// <summary>
    /// Feel 视觉反馈后端。把 Martian.Feedback 的请求映射到预配置的 MMF_Player 预制体。
    /// 不参与时序控制——所有 handle 立即 Complete。
    /// 不播放音频——所有 MMF_Player 的 Audio feedback 应在预制体上禁用。
    /// </summary>
    public sealed class FeelFeedbackBackend : IFeedbackPlaybackBackend
    {
        private readonly Dictionary<string, MMF_Player> _players = new();
        private bool _attached;
        private Transform _host;
        private Canvas _hostCanvas;

        /// <summary>
        /// 注册一个 MMF_Player 到指定的 slot。
        /// slot 名称对应 <see cref="FeelFeedbackSlots"/> 中的常量。
        /// </summary>
        public void RegisterPlayer(string slot, MMF_Player player)
        {
            if (string.IsNullOrEmpty(slot)) return;

            if (player != null)
                _players[slot] = player;
            else
                _players.Remove(slot);
        }

        public bool IsAvailable => _attached && _players.Count > 0;

        // Feel 不参与时序，此事件永不触发。
        public event Action AllPlaybackCompleted
        {
            add { }
            remove { }
        }

        public void Attach(Transform host)
        {
            _attached = host != null;
            _host = host;
            _hostCanvas = host != null ? host.GetComponentInParent<Canvas>() : null;
        }

        public void Configure(FeedbackRuntimeOptions options)
        {
            // Feel 后端不使用 Martian 的 runtime options。
        }

        public FeedbackPlaybackHandle Publish(FeedbackRequest request)
        {
            if (request != null)
            {
                string slot = ResolveSlot(request.Category, request.NumericDelta);
                if (TryResolvePosition(request.Anchor, request.ScreenOffset, request.UseScreenCenterFallback, out var position))
                {
                    PlaySlotAt(slot, position, request.DebugLabel);
                }
                else
                {
                    PlaySlot(slot, request.DebugLabel);
                }
            }

            // Feel 后端的 handle 不参与时序控制，由 Composite 丢弃，返回 null。
            return null;
        }

        public FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request)
        {
            // 序列反馈：只在首步触发一次 Feel 效果，避免连续轰炸。
            if (request != null && request.Steps != null && request.Steps.Count > 0)
            {
                FeedbackStep first = request.Steps[0];
                string slot = ResolveSlot(first.Category, first.Amount);
                Vector2 offset = request.ScreenOffset + first.Offset;
                if (TryResolvePosition(request.Anchor, offset, request.UseScreenCenterFallback, out var position))
                {
                    PlaySlotAt(slot, position, request.DebugLabel);
                }
                else
                {
                    PlaySlot(slot, request.DebugLabel);
                }
            }

            return null;
        }

        public void Clear()
        {
            foreach (var kvp in _players)
            {
                if (kvp.Value != null && kvp.Value.IsPlaying)
                    kvp.Value.StopFeedbacks();
            }
        }

        /// <summary>
        /// 直接按 slot 名称触发 Feel 效果。
        /// 供 UI 层直接调用（如出牌成功、奖励选择），不经过 Composite。
        /// </summary>
        public void PlaySlot(string slot, string debugLabel = null)
        {
            if (string.IsNullOrEmpty(slot)) return;

            if (!_players.TryGetValue(slot, out MMF_Player player) || player == null)
            {
                return;
            }

            player.PlayFeedbacks();
        }

        /// <summary>
        /// 在指定世界坐标播放 slot。用于让 Feel 视觉增强贴近房间、HUD 或卡牌锚点。
        /// </summary>
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

        private static string ResolveSlot(string category, int numericDelta)
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
    }
}

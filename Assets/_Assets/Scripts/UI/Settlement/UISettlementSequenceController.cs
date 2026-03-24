using System.Collections.Generic;
using BaoZuPo.Core;
using BaoZuPo.UI.Common.Sequence;
using Martian.EventBus;
using UnityEngine;

namespace BaoZuPo.UI.Settlement
{
    public class UISettlementSequenceController : MonoBehaviour
    {
        [Header("\u8fd0\u884c\u65f6\u5f15\u7528")]
        [SerializeField] private UISequenceTextController sequencePlayer;

        private void OnEnable()
        {
            EnsureSequencePlayer();
            sequencePlayer.PlaybackCompleted += OnPlaybackCompleted;
            EventBus.Subscribe<GameEvents.SettlementSequenceQueued>(OnSettlementQueued);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.SettlementSequenceQueued>(OnSettlementQueued);
            if (sequencePlayer != null)
            {
                sequencePlayer.PlaybackCompleted -= OnPlaybackCompleted;
                sequencePlayer.HideImmediate();
            }
        }

        public void Enqueue(UISettlementSequenceData data)
        {
            EnsureSequencePlayer();
            if (data == null)
            {
                return;
            }

            sequencePlayer.Enqueue(data.ToPlaybackRequest());
        }

        public void Enqueue(UISequencePlaybackRequest request)
        {
            EnsureSequencePlayer();
            if (request == null)
            {
                return;
            }

            sequencePlayer.Enqueue(request);
        }

        private void OnSettlementQueued(GameEvents.SettlementSequenceQueued payload)
        {
            var data = BuildSequenceData(payload);
            if (data != null)
            {
                Enqueue(data);
            }
        }

        private UISettlementSequenceData BuildSequenceData(GameEvents.SettlementSequenceQueued payload)
        {
            if (payload.Steps == null || payload.Steps.Length == 0)
            {
                return null;
            }

            var anchor = ResolveAnchor(payload);

            var data = new UISettlementSequenceData
            {
                DebugLabel = payload.Title,
                Anchor = anchor,
                UseScreenCenterFallback = payload.SourceKind == GameEvents.SettlementSourceKind.Event || anchor == null,
                ScreenOffset = ResolveScreenOffset(payload.SourceKind),
                GapSeconds = 0.06f,
                Steps = new List<UISequenceStep>(payload.Steps.Length)
            };

            for (int i = 0; i < payload.Steps.Length; i++)
            {
                var step = payload.Steps[i];
                data.Steps.Add(new UISequenceStep
                {
                    Text = FormatStep(step),
                    Color = ResolveStepColor(payload.SourceKind, step, i == payload.Steps.Length - 1),
                    HoldSeconds = i == payload.Steps.Length - 1 ? 0.75f : 0.55f,
                    FadeInSeconds = 0.12f,
                    FadeOutSeconds = 0.14f,
                    Scale = i == payload.Steps.Length - 1 ? 1.08f : 1f,
                    Offset = Vector2.zero
                });
            }

            return data;
        }

        private RectTransform ResolveAnchor(GameEvents.SettlementSequenceQueued payload)
        {
            if (UIManager.Instance == null || UIManager.Instance.boardPanel == null)
            {
                return null;
            }

            return payload.SourceKind switch
            {
                GameEvents.SettlementSourceKind.Room => UIManager.Instance.boardPanel.ResolveRoomAnchor(payload.Room),
                GameEvents.SettlementSourceKind.Contract => UIManager.Instance.boardPanel.ResolveContractAnchor(payload.Card),
                _ => null
            };
        }

        private static Vector2 ResolveScreenOffset(GameEvents.SettlementSourceKind sourceKind)
        {
            return sourceKind switch
            {
                GameEvents.SettlementSourceKind.Room => new Vector2(0f, 140f),
                GameEvents.SettlementSourceKind.Contract => new Vector2(0f, 120f),
                GameEvents.SettlementSourceKind.Event => new Vector2(0f, 48f),
                _ => new Vector2(0f, 96f)
            };
        }

        private static string FormatStep(GameEvents.SettlementStep step)
        {
            if (step.IsMultiplier)
            {
                float multiplier = step.Amount / 100f;
                return $"{step.Label} x{multiplier:0.##}";
            }

            string sign = step.Amount > 0 ? "+" : string.Empty;
            return $"{step.Label} {sign}{step.Amount}";
        }

        private static Color ResolveStepColor(GameEvents.SettlementSourceKind sourceKind, GameEvents.SettlementStep step, bool isFinal)
        {
            if (isFinal)
            {
                return new Color(1f, 0.86f, 0.32f);
            }

            if (step.IsMultiplier)
            {
                return new Color(0.82f, 0.76f, 1f);
            }

            return sourceKind switch
            {
                GameEvents.SettlementSourceKind.Room => new Color(0.58f, 1f, 0.62f),
                GameEvents.SettlementSourceKind.Contract => new Color(0.54f, 0.80f, 1f),
                GameEvents.SettlementSourceKind.Event => new Color(1f, 0.72f, 0.42f),
                _ => Color.white
            };
        }

        private void EnsureSequencePlayer()
        {
            if (sequencePlayer != null)
            {
                return;
            }

            var child = transform.Find("SequenceTextController");
            if (child == null)
            {
                child = new GameObject("SequenceTextController", typeof(RectTransform)).transform;
                child.SetParent(transform, false);
            }

            sequencePlayer = child.GetComponent<UISequenceTextController>();
            if (sequencePlayer == null)
            {
                sequencePlayer = child.gameObject.AddComponent<UISequenceTextController>();
            }
        }

        private void OnPlaybackCompleted()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RefreshAll();
            }
        }
    }
}

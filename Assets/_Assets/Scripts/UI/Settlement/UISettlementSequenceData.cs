using System;
using System.Collections.Generic;
using BaoZuPo.UI.Common.Sequence;
using UnityEngine;

namespace BaoZuPo.UI.Settlement
{
    /// <summary>
    /// 包租婆结算弹字数据。
    /// 这是业务层的轻量传输对象，最终会被转换成通用的顺序弹字请求。
    /// </summary>
    [Serializable]
    public class UISettlementSequenceData
    {
        public string DebugLabel;
        public RectTransform Anchor;
        public bool UseScreenCenterFallback = true;
        public Vector2 ScreenOffset = new Vector2(0f, 96f);
        public float GapSeconds = 0.05f;
        public List<UISequenceStep> Steps = new();

        public UISequencePlaybackRequest ToPlaybackRequest()
        {
            var request = new UISequencePlaybackRequest
            {
                DebugLabel = DebugLabel,
                Anchor = Anchor,
                UseScreenCenterFallback = UseScreenCenterFallback,
                ScreenOffset = ScreenOffset,
                GapSeconds = GapSeconds
            };

            foreach (var step in Steps)
            {
                if (step == null)
                {
                    continue;
                }

                request.Steps.Add(new UISequenceStep
                {
                    Text = step.Text,
                    Color = step.Color,
                    HoldSeconds = step.HoldSeconds,
                    FadeInSeconds = step.FadeInSeconds,
                    FadeOutSeconds = step.FadeOutSeconds,
                    Scale = step.Scale,
                    Offset = step.Offset
                });
            }

            return request;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.UI.Common.Sequence
{
    /// <summary>
    /// 顺序弹字播放请求，定义一系列文字步骤的播放参数和时序。
    /// 包含定位锚点、屏幕偏移、步骤间隔等配置。
    /// 可被克隆以支持队列复用。
    /// </summary>
    [Serializable]
    public class UISequencePlaybackRequest
    {
        public string DebugLabel;
        public RectTransform Anchor;
        public bool UseScreenCenterFallback = true;
        public Vector2 ScreenOffset = new Vector2(0f, 96f);
        public float GapSeconds = 0.05f;
        public List<UISequenceStep> Steps = new();

        public UISequencePlaybackRequest Clone()
        {
            var clone = new UISequencePlaybackRequest
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

                clone.Steps.Add(new UISequenceStep
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

            return clone;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martian.Feedback
{
    [Serializable]
    public class FeedbackRuntimeOptions
    {
        public bool EnableFeedback = true;
        public bool EnableLogs = false;
        public int SortingOrder = 6000;
        public Vector2 DefaultScreenOffset = new Vector2(0f, 96f);
        public Vector2 SequenceScreenOffset = new Vector2(0f, 96f);
        public Vector2 PanelSize = new Vector2(280f, 90f);
        public Vector2 PanelPadding = new Vector2(44f, 20f);
        public Color PanelColor = new Color(0f, 0f, 0f, 0.56f);
        public Color TextColor = Color.white;

        /// <summary>类别 → 颜色映射表。键为 Category 字符串。</summary>
        public Dictionary<string, Color> CategoryColors = new();

        /// <summary>默认颜色（找不到 Category 对应颜色时使用）。</summary>
        public Color DefaultColor = new Color(0.58f, 1f, 0.62f);

        /// <summary>序列最后一步的高亮颜色。</summary>
        public Color FinalStepColor = new Color(1f, 0.86f, 0.32f);

        /// <summary>乘数步骤的颜色。</summary>
        public Color MultiplierColor = new Color(0.82f, 0.76f, 1f);

        public float SingleHoldSeconds = 0.6f;
        public float NormalHoldSeconds = 0.55f;
        public float FinalHoldSeconds = 0.75f;
        public float SingleFadeInSeconds = 0.12f;
        public float SingleFadeOutSeconds = 0.14f;
        public float SingleScale = 1.05f;
        public float FinalScale = 1.08f;
        public float SequenceGapSeconds = 0.06f;
        public float SequenceFadeInSeconds = 0.12f;
        public float SequenceFadeOutSeconds = 0.14f;

        public FeedbackRuntimeOptions Clone()
        {
            var clone = (FeedbackRuntimeOptions)MemberwiseClone();
            clone.CategoryColors = new Dictionary<string, Color>(CategoryColors);
            return clone;
        }
    }
}


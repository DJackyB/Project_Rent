using System;
using UnityEngine;

namespace Martian.Feedback
{
    [Serializable]
    public class FeedbackRuntimeOptions
    {
        public bool EnableFeedback = true;
        public bool EnableMoneyFeedback = true;
        public bool EnableLogs = false;
        public int SortingOrder = 6000;
        public Vector2 DefaultScreenOffset = new Vector2(0f, 96f);
        public Vector2 SequenceScreenOffset = new Vector2(0f, 96f);
        public Vector2 PanelSize = new Vector2(280f, 90f);
        public Vector2 PanelPadding = new Vector2(44f, 20f);
        public Color PanelColor = new Color(0f, 0f, 0f, 0.56f);
        public Color TextColor = Color.white;
        public Color PositiveColor = new Color(0.58f, 1f, 0.62f);
        public Color CostColor = new Color(1f, 0.58f, 0.42f);
        public Color LoanColor = new Color(1f, 0.48f, 0.36f);
        public Color FinalColor = new Color(1f, 0.86f, 0.32f);
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
            return (FeedbackRuntimeOptions)MemberwiseClone();
        }
    }
}

using System;
using UnityEngine;

namespace BaoZuPo.UI.Common.FeedbackPopup
{
    [Serializable]
    public sealed class UIFeedbackPopupStyle
    {
        public Vector2 PanelSize = new Vector2(180f, 48f);
        public Vector2 PanelPadding = new Vector2(20f, 10f);
        public float FontSize = 24f;
        public float FinalFontSize = 28f;
        public Color TextColor = Color.white;
        public Color DefaultPanelColor = new Color(0.08f, 0.1f, 0.16f, 0.9f);
        public Color PositivePanelColor = new Color(0.1f, 0.45f, 0.18f, 0.92f);
        public Color NegativePanelColor = new Color(0.78f, 0.08f, 0.06f, 0.92f);
        public Color MultiplierPanelColor = new Color(0.14f, 0.24f, 0.72f, 0.92f);
        public Color FinalPanelColor = new Color(0.82f, 0.58f, 0.1f, 0.94f);
        public float FadeInSeconds = 0.06f;
        public float PopOvershootSeconds = 0.09f;
        public float SettleSeconds = 0.06f;
        public float HoldSeconds = 0.52f;
        public float FadeOutSeconds = 0.12f;
        public float EntryScale = 0.55f;
        public float OvershootScale = 1.22f;
        public float SettledScale = 1f;
        public float ExitScale = 0.96f;
        public Vector2 EntryOffset = new Vector2(0f, -6f);
        public Vector2 SettleOffset = Vector2.zero;
        public Vector2 ExitOffset = new Vector2(0f, 14f);

        public Color ResolvePanelColor(string category, bool isFinal)
        {
            if (isFinal)
            {
                return FinalPanelColor;
            }

            return category switch
            {
                UIFeedbackPopupCategory.Positive => PositivePanelColor,
                UIFeedbackPopupCategory.Negative => NegativePanelColor,
                UIFeedbackPopupCategory.Multiplier => MultiplierPanelColor,
                UIFeedbackPopupCategory.Final => FinalPanelColor,
                _ => DefaultPanelColor
            };
        }

        public UIFeedbackPopupStyle Clone()
        {
            return (UIFeedbackPopupStyle)MemberwiseClone();
        }
    }
}

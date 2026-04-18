using System;
using UnityEngine;

namespace BaoZuPo.UI.Common.Sequence
{
    /// <summary>
    /// 顺序弹字中的单步定义，包含文本、颜色、动画时长和视觉效果。
    /// 支持淡入/淡出时长、显示持续时间、缩放和位置偏移等参数。
    /// </summary>
    [Serializable]
    public class UISequenceStep
    {
        public string Text;
        public Color Color = Color.white;
        public float HoldSeconds = 0.65f;
        public float FadeInSeconds = 0.12f;
        public float FadeOutSeconds = 0.14f;
        public float Scale = 1f;
        public Vector2 Offset = Vector2.zero;
    }
}

using System;
using UnityEngine;

namespace BaoZuPo.UI.Common.Sequence
{
    /// <summary>
    /// 顺序弹字中的单个步骤。
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

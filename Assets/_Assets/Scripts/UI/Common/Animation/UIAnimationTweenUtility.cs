using BaoZuPo.UI.Common.Sequence;
using DG.Tweening;
using UnityEngine;
using TweenSequence = DG.Tweening.Sequence;

namespace BaoZuPo.UI.Common.Animation
{
    /// <summary>
    /// 补间动画工具集，提供 UI 动画常用方法。
    /// 包含浮动步骤序列构建（淡入/浮起/淡出）和数值脉冲效果。
    /// 使用 DOTween 驱动所有动画。
    /// </summary>
    public static class UIAnimationTweenUtility
    {
        public static TweenSequence BuildFloatingStepSequence(
            RectTransform panelRoot,
            CanvasGroup panelGroup,
            Vector2 anchoredPosition,
            UISequenceStep step,
            bool isFinalStep)
        {
            var sequence = DOTween.Sequence().SetUpdate(true);
            if (panelRoot == null || panelGroup == null || step == null)
            {
                return sequence;
            }

            float fadeInSeconds = Mathf.Max(0f, step.FadeInSeconds);
            float holdSeconds = Mathf.Max(0f, step.HoldSeconds);
            float fadeOutSeconds = Mathf.Max(0f, step.FadeOutSeconds);
            float baseScale = Mathf.Max(0.7f, step.Scale);
            float emphasizedScale = isFinalStep ? Mathf.Max(baseScale, 1.12f) : baseScale;
            Vector2 entryPosition = anchoredPosition + new Vector2(0f, isFinalStep ? -18f : -14f);
            Vector2 settlePosition = anchoredPosition + new Vector2(0f, isFinalStep ? 12f : 8f);
            Vector2 exitPosition = anchoredPosition + new Vector2(0f, isFinalStep ? 30f : 18f);
            float entryScale = emphasizedScale * (isFinalStep ? 0.9f : 0.94f);
            float exitScale = emphasizedScale * (isFinalStep ? 1.08f : 1.03f);

            sequence.AppendCallback(() =>
            {
                panelRoot.gameObject.SetActive(true);
                panelRoot.anchoredPosition = entryPosition;
                panelRoot.localScale = Vector3.one * entryScale;
                panelGroup.alpha = 0f;
            });

            if (fadeInSeconds > 0f)
            {
                sequence.Append(panelGroup.DOFade(1f, fadeInSeconds).SetEase(Ease.OutQuad));
                sequence.Join(panelRoot.DOAnchorPos(settlePosition, fadeInSeconds).SetEase(Ease.OutCubic));
                sequence.Join(panelRoot.DOScale(emphasizedScale, fadeInSeconds).SetEase(isFinalStep ? Ease.OutBack : Ease.OutQuad));
            }
            else
            {
                sequence.AppendCallback(() =>
                {
                    panelGroup.alpha = 1f;
                    panelRoot.anchoredPosition = settlePosition;
                    panelRoot.localScale = Vector3.one * emphasizedScale;
                });
            }

            if (holdSeconds > 0f)
            {
                sequence.AppendInterval(holdSeconds);
            }

            if (fadeOutSeconds > 0f)
            {
                sequence.Append(panelGroup.DOFade(0f, fadeOutSeconds).SetEase(Ease.InQuad));
                sequence.Join(panelRoot.DOAnchorPos(exitPosition, fadeOutSeconds).SetEase(Ease.InCubic));
                sequence.Join(panelRoot.DOScale(exitScale, fadeOutSeconds).SetEase(Ease.OutQuad));
            }
            else
            {
                sequence.AppendCallback(() =>
                {
                    panelGroup.alpha = 0f;
                    panelRoot.anchoredPosition = exitPosition;
                    panelRoot.localScale = Vector3.one * exitScale;
                });
            }

            return sequence;
        }

        public static Tween PunchScale(RectTransform target, float punchScale = 0.08f, float duration = 0.18f, int vibrato = 8, float elasticity = 0.55f)
        {
            if (target == null)
            {
                return null;
            }

            target.DOKill(false);
            target.localScale = Vector3.one;
            return target.DOPunchScale(Vector3.one * punchScale, duration, vibrato, elasticity).SetUpdate(true);
        }

        public static Tween PunchScalePreserveBase(RectTransform target, float punchScale = 0.06f, float duration = 0.16f, int vibrato = 7, float elasticity = 0.5f)
        {
            if (target == null)
            {
                return null;
            }

            target.DOKill(false);
            return target.DOPunchScale(Vector3.one * punchScale, duration, vibrato, elasticity).SetUpdate(true);
        }
    }
}

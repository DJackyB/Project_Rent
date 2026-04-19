using System;
using DG.Tweening;
using Martian.Localization;
using TMPro;
using UnityEngine;

namespace BaoZuPo.UI.Common.FlyingNumber
{
    /// <summary>
    /// 飞行数字视图，沿三次贝塞尔弧线从源点飞向目标点。
    /// 到达目标点时触发回调（通常用于更新金钱显示），随后淡出销毁。
    /// 由 UIFeedbackPopupLayer 创建和管理，支持同时多个飞行实例并行。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIFlyingNumberView : MonoBehaviour
    {
        public static Action<TMP_Text> DefaultTextConfigurator { get; set; } = ApplyProjectLocalizedFont;

        private RectTransform _rect;
        private CanvasGroup _group;
        private TextMeshProUGUI _label;
        private Tween _sequence;
        private Action _onArrived;

        public void Play(
            Vector2 sourceLocalPos,
            Vector2 targetLocalPos,
            string text,
            Color color,
            float fontSize,
            float flySeconds,
            float arcHeight,
            float holdAfterArrive,
            float fadeOutSeconds,
            Action onArrived)
        {
            EnsureView();

            _label.text = text;
            _label.color = color;
            _label.fontSize = fontSize;
            DefaultTextConfigurator?.Invoke(_label);

            _onArrived = onArrived;
            _rect.anchoredPosition = sourceLocalPos;
            _rect.localScale = Vector3.one * 0.6f;
            _group.alpha = 0f;

            Vector2 p0 = sourceLocalPos;
            Vector2 p3 = targetLocalPos;
            Vector2 p1 = p0 + new Vector2(0f, arcHeight);
            Vector2 p2 = p3 + new Vector2(0f, arcHeight * 0.55f);

            float safeFly = Mathf.Max(0.05f, flySeconds);
            float safeHold = Mathf.Max(0f, holdAfterArrive);
            float safeFadeOut = Mathf.Max(0.02f, fadeOutSeconds);

            _sequence?.Kill(false);
            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Append(_group.DOFade(1f, 0.08f).SetEase(Ease.OutQuad));
            sequence.Join(_rect.DOScale(1.1f, 0.14f).SetEase(Ease.OutBack));
            sequence.Append(DOVirtual.Float(0f, 1f, safeFly, t =>
            {
                if (_rect != null)
                {
                    _rect.anchoredPosition = CubicBezier(p0, p1, p2, p3, t);
                }
            }).SetEase(Ease.InOutCubic));
            sequence.Join(_rect.DOScale(0.95f, safeFly * 0.5f).SetEase(Ease.InQuad).SetDelay(safeFly * 0.5f));
            sequence.AppendCallback(() =>
            {
                var callback = _onArrived;
                _onArrived = null;
                callback?.Invoke();
            });
            if (safeHold > 0f)
            {
                sequence.AppendInterval(safeHold);
            }

            sequence.Append(_group.DOFade(0f, safeFadeOut).SetEase(Ease.InQuad));
            sequence.Join(_rect.DOScale(0.6f, safeFadeOut).SetEase(Ease.InQuad));
            sequence.OnComplete(() =>
            {
                if (this != null && gameObject != null)
                {
                    Destroy(gameObject);
                }
            });

            _sequence = sequence;
        }

        private void OnDestroy()
        {
            _sequence?.Kill(false);
            _sequence = null;
        }

        private void EnsureView()
        {
            _rect = transform as RectTransform;
            if (_rect == null)
            {
                _rect = gameObject.AddComponent<RectTransform>();
            }

            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(240f, 64f);

            _group = GetComponent<CanvasGroup>();
            if (_group == null)
            {
                _group = gameObject.AddComponent<CanvasGroup>();
            }

            _group.blocksRaycasts = false;
            _group.interactable = false;

            _label = GetComponentInChildren<TextMeshProUGUI>(true);
            if (_label == null)
            {
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(transform, false);

                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                _label = labelObject.GetComponent<TextMeshProUGUI>();
                _label.alignment = TextAlignmentOptions.Center;
                _label.textWrappingMode = TextWrappingModes.NoWrap;
                _label.raycastTarget = false;
                _label.fontStyle = FontStyles.Bold;
            }
        }

        private static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;
            return uu * u * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + tt * t * p3;
        }

        private static void ApplyProjectLocalizedFont(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            try
            {
                LocalizationFontUtility.ApplyToText(text);
            }
            catch
            {
                var font = UnityEngine.Resources.Load<TMP_FontAsset>(
                    "Fonts/SourceHanSansSC/SourceHanSansSC-Regular SDF");
                if (font != null)
                {
                    text.font = font;
                }
            }
        }
    }
}

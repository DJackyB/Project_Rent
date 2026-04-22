using BaoZuPo.Core;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using BaoZuPo.UI.Common.Animation;
using DG.Tweening;
using Martian.EventBus;
using TMPro;
using UnityEngine;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 顶部信息条，显示回合数、当前金钱和累计花费。
    /// 支持金钱的延迟显示模式，用于结算演示期间平滑动画更新金钱数值。
    /// 提供金钱显示目标锚点供转账动画使用。
    /// </summary>
    public class UITopBar : MonoBehaviour
    {
        [Header("Optional Scene References")]
        public TextMeshProUGUI turnText;
        public TextMeshProUGUI moneyText;
        public TextMeshProUGUI totalSpent;
        public TextMeshProUGUI nextLoanDueText;
        public TextMeshProUGUI nextLoanAmountText;
        [SerializeField] private RectTransform moneyTargetAnchor;
        [SerializeField] private float playCostPopupVerticalGap = 18f;
        [SerializeField] private float settlementTotalPopupVerticalGap = 40f;
        [SerializeField] private bool useRuntimeGeneratedLayout;
        [SerializeField] private float moneyCountDuration = 0.5f;

        private bool _deferredMoneyDisplay;
        private int _displayedMoney;
        private int _authoritativeMoney;
        private float _displayedMoneyFloat;
        private Tween _moneyCountTween;

        public bool IsDeferredMoneyDisplayActive => _deferredMoneyDisplay;

        public RectTransform MoneyTargetAnchor => moneyTargetAnchor != null ? moneyTargetAnchor : moneyText != null ? moneyText.rectTransform : null;
        public float PlayCostPopupVerticalGap => playCostPopupVerticalGap;
        public float SettlementTotalPopupVerticalGap => settlementTotalPopupVerticalGap;

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
            EventBus.Subscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Subscribe<GameEvents.LoanPayment>(OnLoanPayment);
        }

        private void Start()
        {
            EnsureHudLayout();
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
            EventBus.Unsubscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Unsubscribe<GameEvents.LoanPayment>(OnLoanPayment);
        }

        public void Refresh()
        {
            EnsureHudLayout();
            var turnManager = TurnManager.Instance;
            var moneyManager = MoneyManager.Instance;
            RefreshTurn(turnManager != null ? turnManager.CurrentTurn : 0);
            RefreshMoney(moneyManager != null ? moneyManager.CurrentMoney : _authoritativeMoney);
            RefreshSummary();
            RefreshLoanPreview();
        }

        public void RefreshTurn(int turn)
        {
            if (turnText != null)
            {
                turnText.text = GameText.Turn(turn);
            }
        }

        public void RefreshMoney(int money)
        {
            _authoritativeMoney = money;

            if (_deferredMoneyDisplay)
            {
                return;
            }

            _displayedMoney = money;
            UpdateMoneyLabel();
        }

        public void BeginDeferredMoneyDisplay(int startValue)
        {
            EnsureHudLayout();
            _moneyCountTween?.Kill(false);
            _moneyCountTween = null;
            _deferredMoneyDisplay = true;
            _authoritativeMoney = startValue;
            _displayedMoney = startValue;
            _displayedMoneyFloat = startValue;
            UpdateMoneyLabel();
        }

        public void CommitDisplayedDelta(int delta)
        {
            EnsureHudLayout();

            if (_deferredMoneyDisplay)
            {
                _displayedMoney += delta;
                UpdateMoneyLabel();
                PlayMoneyPulse();
                return;
            }

            RefreshMoney(MoneyManager.Instance.CurrentMoney);
            PlayMoneyPulse();
        }

        public void EndDeferredMoneyDisplay(System.Action onCountUpComplete = null)
        {
            int target = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : _authoritativeMoney;
            _deferredMoneyDisplay = false;
            _authoritativeMoney = target;
            if (_displayedMoney == target)
            {
                _moneyCountTween?.Kill(false);
                _moneyCountTween = null;
                UpdateMoneyLabel();
                onCountUpComplete?.Invoke();
                return;
            }
            TweenMoneyLabel(_displayedMoney, target, onCountUpComplete);
            _displayedMoney = target;
        }

        private void UpdateMoneyLabel()
        {
            if (moneyText != null)
            {
                moneyText.text = GameText.Money(_displayedMoney);
            }
        }

        public void RefreshSummary()
        {
            if (totalSpent != null)
            {
                totalSpent.text = GameText.Spent(MoneyManager.Instance != null ? MoneyManager.Instance.TotalSpent : 0);
            }
        }

        public void RefreshLoanPreview()
        {
            string dueLabel = GameText.LoanPreviewUnavailable;
            string amountLabel = GameText.LoanPreviewUnavailable;

            if (TurnManager.Instance != null
                && TurnManager.Instance.TryGetNextLoanPreview(out int dueTurn, out int amount))
            {
                dueLabel = GameText.NextLoanDue(dueTurn);
                amountLabel = GameText.NextLoanAmount(amount);
            }

            if (nextLoanDueText != null)
            {
                nextLoanDueText.text = dueLabel;
            }

            if (nextLoanAmountText != null)
            {
                nextLoanAmountText.text = amountLabel;
            }
        }

        private void OnMoneyChanged(GameEvents.MoneyChanged e)
        {
            _authoritativeMoney = e.NewValue;
            if (!_deferredMoneyDisplay)
            {
                RefreshMoney(e.NewValue);
                PlayMoneyPulse();
            }

            RefreshSummary();
        }

        private void OnTurnStarted(GameEvents.TurnStarted e)
        {
            RefreshTurn(e.TurnNumber);
            RefreshLoanPreview();
        }

        private void OnLoanPayment(GameEvents.LoanPayment e)
        {
            RefreshLoanPreview();
        }

        private void EnsureHudLayout()
        {
            var rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                return;
            }

            bool createdTurnText = false;
            bool createdDeckText = false;
            bool createdMoneyText = false;

            if (turnText == null)
            {
                turnText = CreateRuntimeLabel(rootCanvas.transform, "TurnSummary");
                createdTurnText = true;
            }

            if (totalSpent == null)
            {
                totalSpent = CreateRuntimeLabel(rootCanvas.transform, "SpendSummary");
                createdDeckText = true;
            }

            if (moneyText == null)
            {
                moneyText = CreateRuntimeLabel(rootCanvas.transform, "MoneyHUD");
                createdMoneyText = true;
            }

            if (useRuntimeGeneratedLayout || createdTurnText)
            {
                turnText.transform.SetParent(rootCanvas.transform, false);
                ApplyTopLayout(turnText.rectTransform, new Vector2(-120f, -20f), TextAlignmentOptions.MidlineLeft);
            }

            if (useRuntimeGeneratedLayout || createdDeckText)
            {
                totalSpent.transform.SetParent(rootCanvas.transform, false);
                ApplyTopLayout(totalSpent.rectTransform, new Vector2(120f, -20f), TextAlignmentOptions.MidlineRight);
            }

            if (useRuntimeGeneratedLayout || createdMoneyText)
            {
                moneyText.transform.SetParent(rootCanvas.transform, false);
                ApplyMoneyLayout(moneyText.rectTransform);
            }

            if (moneyTargetAnchor == null)
            {
                moneyTargetAnchor = moneyText.rectTransform;
            }
        }

        private static void ApplyTopLayout(RectTransform rect, Vector2 anchoredPosition, TextAlignmentOptions alignment)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(220f, 32f);

            var label = rect.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.alignment = alignment;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.fontSize = 24f;
                label.raycastTarget = false;
            }
        }

        private static void ApplyMoneyLayout(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(24f, 24f);
            rect.sizeDelta = new Vector2(240f, 36f);

            var label = rect.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.alignment = TextAlignmentOptions.BottomLeft;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.fontSize = 28f;
                label.raycastTarget = false;
            }
        }

        private static TextMeshProUGUI CreateRuntimeLabel(Transform parent, string name)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontSize = 24f;
            label.raycastTarget = false;
            return label;
        }

        private void TweenMoneyLabel(int from, int to, System.Action onComplete = null)
        {
            if (moneyText == null)
            {
                onComplete?.Invoke();
                return;
            }
            _moneyCountTween?.Kill(false);
            _displayedMoneyFloat = from;
            _moneyCountTween = DOTween.To(
                () => _displayedMoneyFloat,
                x =>
                {
                    _displayedMoneyFloat = x;
                    moneyText.text = GameText.Money(Mathf.RoundToInt(x));
                },
                (float)to,
                moneyCountDuration
            ).SetEase(Ease.OutQuart).SetUpdate(true).OnComplete(() =>
            {
                _moneyCountTween = null;
                PlayMoneyPulse();
                onComplete?.Invoke();
            });
        }

        private void PlayMoneyPulse()
        {
            if (moneyText == null)
            {
                return;
            }

            UIAnimationTweenUtility.PunchScalePreserveBase(moneyText.rectTransform, 0.06f, 0.18f);
        }
    }
}

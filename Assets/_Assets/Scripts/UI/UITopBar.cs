using BaoZuPo.Core;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using BaoZuPo.UI.Common.Animation;
using Martian.EventBus;
using TMPro;
using UnityEngine;

namespace BaoZuPo.UI
{
    public class UITopBar : MonoBehaviour
    {
        [Header("\u53ef\u9009\u573a\u666f\u5f15\u7528")]
        public TextMeshProUGUI turnText;
        public TextMeshProUGUI moneyText;
        public TextMeshProUGUI deckText;
        [SerializeField] private RectTransform moneyTargetAnchor;

        private bool _deferredMoneyDisplay;
        private int _displayedMoney;
        private int _authoritativeMoney;

        public bool IsDeferredMoneyDisplayActive => _deferredMoneyDisplay;

        public RectTransform MoneyTargetAnchor => moneyTargetAnchor != null ? moneyTargetAnchor : moneyText != null ? moneyText.rectTransform : null;

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
            EventBus.Subscribe<GameEvents.TurnStarted>(OnTurnStarted);
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
        }

        public void Refresh()
        {
            EnsureHudLayout();
            var turnManager = TurnManager.Instance;
            var moneyManager = MoneyManager.Instance;
            RefreshTurn(turnManager != null ? turnManager.CurrentTurn : 0);
            RefreshMoney(moneyManager != null ? moneyManager.CurrentMoney : _authoritativeMoney);
            RefreshSummary();
        }

        public void RefreshTurn(int turn)
        {
            UIFontCatalog.ApplyToText(turnText);
            if (turnText != null)
            {
                turnText.text = UIStrings.Turn(turn);
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
            _deferredMoneyDisplay = true;
            _authoritativeMoney = startValue;
            _displayedMoney = startValue;
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

        public void EndDeferredMoneyDisplay()
        {
            _deferredMoneyDisplay = false;
            RefreshMoney(MoneyManager.Instance.CurrentMoney);
        }

        private void UpdateMoneyLabel()
        {
            UIFontCatalog.ApplyToText(moneyText);
            if (moneyText != null)
            {
                moneyText.text = UIStrings.Money(_displayedMoney);
            }
        }

        public void RefreshSummary()
        {
            UIFontCatalog.ApplyToText(deckText);
            if (deckText != null)
            {
                deckText.text = UIStrings.Spent(MoneyManager.Instance != null ? MoneyManager.Instance.TotalSpent : 0);
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
        }

        private void EnsureHudLayout()
        {
            var rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                return;
            }

            if (turnText == null)
            {
                turnText = CreateRuntimeLabel(rootCanvas.transform, "TurnSummary");
            }

            if (deckText == null)
            {
                deckText = CreateRuntimeLabel(rootCanvas.transform, "SpendSummary");
            }

            if (moneyText == null)
            {
                moneyText = CreateRuntimeLabel(rootCanvas.transform, "MoneyHUD");
            }

            turnText.transform.SetParent(rootCanvas.transform, false);
            deckText.transform.SetParent(rootCanvas.transform, false);
            moneyText.transform.SetParent(rootCanvas.transform, false);
            UIFontCatalog.ApplyToText(turnText);
            UIFontCatalog.ApplyToText(deckText);
            UIFontCatalog.ApplyToText(moneyText);

            ApplyTopLayout(turnText.rectTransform, new Vector2(-120f, -20f), TextAlignmentOptions.MidlineLeft);
            ApplyTopLayout(deckText.rectTransform, new Vector2(120f, -20f), TextAlignmentOptions.MidlineRight);
            ApplyMoneyLayout(moneyText.rectTransform);

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
            label.font = UIFontCatalog.GetPreferredFontAsset();
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontSize = 24f;
            label.raycastTarget = false;
            return label;
        }

        private void PlayMoneyPulse()
        {
            if (moneyText == null)
            {
                return;
            }

            UIAnimationTweenUtility.PunchScale(moneyText.rectTransform, 0.06f, 0.18f);
        }
    }
}

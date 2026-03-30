using UnityEngine;
using TMPro;
using BaoZuPo.Localization;
using Martian.Localization;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 游戏结束弹窗
    /// </summary>
    public class UIGameOverPanel : MonoBehaviour
    {
        [Header("UI 引用")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI infoText;
        public GameObject panel;

        private bool _hasShownResult;
        private int _lastTotalTurns;
        private int _lastFinalMoney;

        private void Start()
        {
            LocalizationFontUtility.ApplyToChildren(transform);
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void Show(int totalTurns, int finalMoney)
        {
            _hasShownResult = true;
            _lastTotalTurns = totalTurns;
            _lastFinalMoney = finalMoney;

            if (panel != null)
            {
                panel.SetActive(true);
            }

            LocalizationFontUtility.ApplyToText(titleText);
            LocalizationFontUtility.ApplyToText(infoText);

            if (titleText != null)
            {
                titleText.text = GameText.GameOverTitle;
            }

            if (infoText != null)
            {
                infoText.text = GameText.GameOverInfo(totalTurns, finalMoney);
            }
        }

        public void RefreshLocalization()
        {
            LocalizationFontUtility.ApplyToChildren(transform);
            if (_hasShownResult)
            {
                Show(_lastTotalTurns, _lastFinalMoney);
            }
        }
    }
}

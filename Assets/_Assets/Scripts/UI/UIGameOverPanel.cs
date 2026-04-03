using TMPro;
using UnityEngine;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 游戏结束屏幕，显示最终回合数和金钱成绩。
    /// 在游戏结束时激活并填充结算数据。
    /// </summary>
    public class UIGameOverPanel : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI infoText;
        public GameObject panel;

        private void Start()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void Show(int totalTurns, int finalMoney)
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = GameText.GameOverTitle;
            }

            if (infoText != null)
            {
                infoText.text = GameText.GameOverInfo(totalTurns, finalMoney);
            }
        }
    }
}

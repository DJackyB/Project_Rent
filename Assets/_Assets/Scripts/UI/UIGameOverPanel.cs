using UnityEngine;
using TMPro;

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

        private void Start()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        public void Show(int totalTurns, int finalMoney)
        {
            if (panel != null)
                panel.SetActive(true);

            if (titleText != null)
                titleText.text = "Game Over";

            if (infoText != null)
                infoText.text = $"You survived {totalTurns} turns\nFinal Money: ${finalMoney}";
        }
    }
}

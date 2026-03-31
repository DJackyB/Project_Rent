using TMPro;
using UnityEngine;

namespace BaoZuPo.UI
{
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

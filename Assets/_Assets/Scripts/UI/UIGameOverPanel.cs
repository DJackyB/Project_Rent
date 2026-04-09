using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        public Button playAgainButton;
        public TextMeshProUGUI playAgainButtonText;

        private bool _isShowing;

        private void Awake()
        {
            RegisterPlayAgainButton();

            if (!_isShowing)
            {
                Hide();
            }
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        public void Show(int totalTurns, int finalMoney)
        {
            _isShowing = true;
            gameObject.SetActive(true);
            _isShowing = false;
            RegisterPlayAgainButton();

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

            if (playAgainButtonText != null)
            {
                playAgainButtonText.text = GameText.GameOverPlayAgain;
            }
        }

        public void PlayAgain()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                SceneManager.LoadScene(activeScene.name);
            }
        }

        private void RegisterPlayAgainButton()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveListener(PlayAgain);
                playAgainButton.onClick.AddListener(PlayAgain);
            }
        }
    }
}

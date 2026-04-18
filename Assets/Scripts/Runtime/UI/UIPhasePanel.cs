using BaoZuPo.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 阶段指示器面板，显示当前游戏阶段并提供结束行动阶段的按钮。
    /// 在行动阶段时激活"结束回合"按钮，在其他阶段显示"等待"按钮。
    /// 支持运行时动态创建按钮及其文本标签。
    /// </summary>
    public class UIPhasePanel : MonoBehaviour
    {
        [Header("Optional Scene References")]
        public TextMeshProUGUI phaseText;
        public Button endTurnButton;
        public TextMeshProUGUI buttonText;

        private void OnEnable()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }
        }

        private void OnDisable()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
            }
        }

        public void UpdatePhase(string phaseName)
        {
            if (phaseText != null)
            {
                phaseText.text = GameText.PhaseName(phaseName);
            }

            bool isAction = phaseName == "Action";
            if (endTurnButton != null)
            {
                endTurnButton.interactable = isAction;
            }

            if (buttonText != null)
            {
                buttonText.text = isAction ? GameText.EndTurnButton : GameText.WaitingButton;
            }
        }

        private void OnEndTurnClicked()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.EndActionPhase();
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BaoZuPo.GameFlow;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 阶段信息面板 + 结束回合按钮
    /// </summary>
    public class UIPhasePanel : MonoBehaviour
    {
        [Header("UI 引用")]
        public TextMeshProUGUI phaseText;
        public Button endTurnButton;
        public TextMeshProUGUI buttonText;

        private void Start()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }
        }

        public void UpdatePhase(string phaseName)
        {
            string displayName = phaseName switch
            {
                "Prepare" => "Prepare Phase",
                "Action" => "Action Phase",
                "Settle" => "Settle Phase",
                _ => phaseName
            };

            if (phaseText != null)
                phaseText.text = displayName;

            // 只有行动阶段才能点击结束回合
            bool isAction = phaseName == "Action";
            if (endTurnButton != null)
                endTurnButton.interactable = isAction;

            if (buttonText != null)
                buttonText.text = isAction ? "End Turn" : "Waiting...";
        }

        private void OnEndTurnClicked()
        {
            TurnManager.Instance.EndActionPhase();
        }
    }
}

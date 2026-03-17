using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BaoZuPo.Card;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 单张卡牌视图（Prefab 脚本）
    /// 显示卡牌信息，点击选中准备打出。
    /// </summary>
    public class UICardView : MonoBehaviour
    {
        [Header("UI 引用")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI typeText;
        public TextMeshProUGUI descText;
        public TextMeshProUGUI statsText;
        public Button cardButton;
        public Image background;

        [Header("颜色配置")]
        public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        public Color selectedColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        public Color tenantColor = new Color(0.2f, 0.6f, 0.3f, 0.9f);
        public Color equipmentColor = new Color(0.6f, 0.4f, 0.2f, 0.9f);
        public Color eventColor = new Color(0.5f, 0.2f, 0.6f, 0.9f);

        public CardInstance Card { get; private set; }
        private UIHandPanel _handPanel;

        public void Setup(CardInstance card, UIHandPanel panel)
        {
            Card = card;
            _handPanel = panel;

            if (nameText != null) nameText.text = card.Data.cardName;
            if (costText != null) costText.text = $"￥{card.Data.cost}";
            if (descText != null) descText.text = card.Data.description;

            // 类型标签
            if (typeText != null)
            {
                switch (card.Data.cardType)
                {
                    case CardType.Tenant: typeText.text = "Tenant"; break;
                    case CardType.Equipment: typeText.text = "Equipment"; break;
                    case CardType.Event: typeText.text = "Event"; break;
                }
            }

            // 耐久/等待信息
            if (statsText != null)
            {
                string stats = "";
                if (card.Data.durability > 0) stats += $"Durability:{card.CurrentDurability} ";
                if (card.Data.waitTurns > 0) stats += $"Wait:{card.CurrentWait}";
                statsText.text = stats;
            }

            // 根据类型设置底色
            if (background != null)
            {
                normalColor = card.Data.cardType switch
                {
                    CardType.Tenant => tenantColor,
                    CardType.Equipment => equipmentColor,
                    CardType.Event => eventColor,
                    _ => normalColor
                };
                background.color = normalColor;
            }

            // 点击事件
            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            if (_handPanel != null)
            {
                // 如果是事件卡，直接打出
                if (Card.Data.cardType == CardType.Event)
                {
                    GameFlow.TurnManager.Instance.PlayCard(Card, null);
                    UIManager.Instance.RefreshAll();
                }
                else
                {
                    _handPanel.SelectCard(Card);
                }
            }
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
            {
                background.color = selected ? selectedColor : normalColor;
            }
        }
    }
}

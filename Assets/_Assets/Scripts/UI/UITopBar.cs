using UnityEngine;
using TMPro;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using BaoZuPo.Deck;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 顶部信息栏
    /// 显示回合数、当前金钱、牌堆剩余。
    /// </summary>
    public class UITopBar : MonoBehaviour
    {
        [Header("UI 引用")]
        public TextMeshProUGUI turnText;
        public TextMeshProUGUI moneyText;
        public TextMeshProUGUI deckText;

        public void Refresh()
        {
            RefreshTurn(TurnManager.Instance.CurrentTurn);
            RefreshMoney(MoneyManager.Instance.CurrentMoney);
            RefreshDeck();
        }

        public void RefreshTurn(int turn)
        {
            if (turnText != null)
                turnText.text = $"回合 {turn}";
        }

        public void RefreshMoney(int money)
        {
            if (moneyText != null)
                moneyText.text = $"￥{money}";
        }

        public void RefreshDeck()
        {
            if (deckText != null)
                deckText.text = $"牌堆 {DeckManager.Instance.DrawPileCount}";
        }
    }
}

using System.Collections;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Deck;
using BaoZuPo.UI;
using Martian.EventBus;
using UnityEngine;

namespace BaoZuPo.Core
{
    /// <summary>
    /// 临时测试驱动：验证弃牌/销毁/抽牌/洗牌动画与 popup。
    /// 在 Inspector 里右键调用方法；测试完删掉整个文件。
    /// </summary>
    public class DeckAnimationTestDriver : MonoBehaviour
    {
        // ── 随机弃一张手牌（触发弃牌动画）─────────��──────────────────────
        [ContextMenu("Test / 随机弃一张手牌")]
        private void DiscardRandomHandCard()
        {
            var deck = DeckManager.Instance;
            if (deck == null || deck.HandCount == 0)
            {
                Debug.LogWarning("[TestDriver] 手牌为空。");
                return;
            }

            int index = Random.Range(0, deck.HandCount);
            var card = deck.Hand[index];
            Debug.Log($"[TestDriver] 弃牌开始：{card.Data.cardName}");

            deck.RemoveFromHand(card);
            deck.SendToDiscard(card);

            var handPanel = UIManager.Instance?.handPanel;
            Debug.Log($"[TestDriver] handPanel={handPanel != null}, views={handPanel?.CardViewsCount ?? -1}");
            if (handPanel != null)
            {
                var view = handPanel.FindView(card);
                Debug.Log($"[TestDriver] FindView result={(view != null ? view.name : "null")}");
                handPanel.PlayDiscardAnimation(card);
            }

            Debug.Log($"[TestDriver] 弃牌完成：{card.Data.cardName}");
        }

        // ── 随机销毁一张场上的牌（触发销毁动画）─────────────────────────
        [ContextMenu("Test / 随机销毁一张场上的牌")]
        private void DestroyRandomBoardCard()
        {
            var board = BoardManager.Instance;
            if (board == null)
            {
                Debug.LogWarning("[TestDriver] BoardManager 未就绪。");
                return;
            }

            var fieldCards = board.GetAllFieldCards();
            if (fieldCards == null || fieldCards.Count == 0)
            {
                Debug.LogWarning("[TestDriver] 场上没有卡牌。");
                return;
            }

            int index = Random.Range(0, fieldCards.Count);
            var card = fieldCards[index];
            Debug.Log($"[TestDriver] 销毁开始：{card.Data.cardName}");

            card.MarkDestroyed();
            EventBus.Publish(new GameEvents.CardDestroyed { Card = card, TriggeredByDurability = true });
            board.CleanupDestroyedCards();

            Debug.Log($"[TestDriver] 销毁：{card.Data.cardName}");
        }

        // ── 按正式逻辑抽一张牌（含入场动画）───────────────────────────���
        [ContextMenu("Test / 正式抽一张牌（含动画）")]
        private void DrawOneCardWithAnimation()
        {
            StartCoroutine(DrawOneCoroutine());
        }

        private IEnumerator DrawOneCoroutine()
        {
            var deck = DeckManager.Instance;
            if (deck == null)
            {
                Debug.LogWarning("[TestDriver] DeckManager 未就绪。");
                yield break;
            }

            if (deck.DrawPileCount == 0 && deck.DiscardPileCount > 0)
            {
                Debug.Log("[TestDriver] 抽牌堆耗尽，触发洗牌……");
                ShowShufflePopup();
                deck.ShuffleDiscardIntoDraw();
                yield return new WaitForSeconds(0.35f);
            }

            var drawn = deck.Draw(1);
            if (drawn == null || drawn.Count == 0)
            {
                Debug.LogWarning("[TestDriver] 抽不到牌（手牌已满或牌堆为空）。");
                yield break;
            }

            var handPanel = UIManager.Instance?.handPanel;
            Debug.Log($"[TestDriver] 准备抽牌动画，handPanel={handPanel != null}, animLayerReady={handPanel?.IsAnimationLayerReady ?? false}");
            if (handPanel != null)
            {
                yield return handPanel.PlayIncomingCard(drawn[0], UIHandIncomingAnimationKind.TurnDraw);
            }

            Debug.Log($"[TestDriver] 抽到：{drawn[0].Data.cardName}");
        }

        // ── 直接重新洗牌（弃牌堆 → 抽牌堆）──────────────────────────────
        [ContextMenu("Test / 直接重新洗牌")]
        private void ReshuffleNow()
        {
            var deck = DeckManager.Instance;
            if (deck == null)
            {
                Debug.LogWarning("[TestDriver] DeckManager 未就绪。");
                return;
            }

            if (deck.DiscardPileCount == 0)
            {
                Debug.LogWarning("[TestDriver] 弃牌堆为空，无法洗牌。");
                return;
            }

            var layer = UI.Common.FeedbackPopup.UIFeedbackPopupLayer.GetOrCreate(null);
            Debug.Log($"[TestDriver] popup layer={(layer != null ? layer.name : "null")}, text='{GameText.DeckShuffled}'");
            ShowShufflePopup();
            deck.ShuffleDiscardIntoDraw();
            Debug.Log($"[TestDriver] 洗牌完成，抽牌堆：{deck.DrawPileCount} 张。");
        }

        private static void ShowShufflePopup()
        {
            var layer = UI.Common.FeedbackPopup.UIFeedbackPopupLayer.GetOrCreate(null);
            if (layer == null) return;

            var style = layer.DefaultStyle.Clone();
            style.HoldSeconds = 0.6f;

            var view = layer.Show(new UI.Common.FeedbackPopup.UIFeedbackPopupRequest
            {
                Text = GameText.DeckShuffled,
                Category = UI.Common.FeedbackPopup.UIFeedbackPopupCategory.Default,
                ScreenOffset = Vector2.zero,
                UseScreenCenterFallback = true,
                Style = style,
            });
            Debug.Log($"[TestDriver] popup view={(view != null ? view.name : "null (Show returned null)")}");
        }
    }
}

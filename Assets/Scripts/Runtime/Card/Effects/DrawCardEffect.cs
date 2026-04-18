using BaoZuPo.Card;
using BaoZuPo.Core;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 抽取额外卡牌。
    /// 格式：DrawCard;数量 或 DrawCard;数量;牌库ID
    /// </summary>
    public class DrawCardEffect : ICardEffect
    {
        private readonly int _count;
        private readonly string _libraryId;

        public DrawCardEffect(int count, string libraryId = null)
        {
            _count = count;
            _libraryId = string.IsNullOrWhiteSpace(libraryId) ? null : libraryId;

            if (_libraryId != null && CardLibraryDatabase.IsLoaded && !CardLibraryDatabase.TryGetById(_libraryId, out _))
            {
                throw new System.ArgumentException($"Library '{_libraryId}' does not exist.");
            }
        }

        // 执行：根据是否指定牌库ID，从默认牌库或特定牌库中抽取指定数量的卡牌。
        public void Execute(CardInstance card, GameContext context)
        {
            var deckManager = context?.DeckManager ?? Deck.DeckManager.Instance;
            if (deckManager == null)
            {
                Debug.LogWarning("[Effect] Failed to draw cards because DeckManager is unavailable.");
                return;
            }

            if (_libraryId == null)
            {
                deckManager.Draw(_count);
                Debug.Log($"[Effect] Drew {_count} extra cards.");
                return;
            }

            var library = ResolveLibrary();
            if (library == null)
            {
                Debug.LogWarning($"[Effect] Failed to resolve draw library for effect on {card?.Data?.cardName ?? "unknown"}.");
                return;
            }

            deckManager.DrawFromLibrary(library, _count);
            Debug.Log($"[Effect] Drew {_count} extra cards from library '{library.DisplayName}'.");
        }

        // 从牌库数据库根据 ID 解析具体牌库对象。
        private CardLibrary ResolveLibrary()
        {
            return CardLibraryDatabase.GetById(_libraryId);
        }
    }
}

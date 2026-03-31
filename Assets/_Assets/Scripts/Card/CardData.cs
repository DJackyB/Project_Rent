using BaoZuPo.GameFlow;
using UnityEngine;

namespace BaoZuPo.Card
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "BaoZuPo/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Unique card ID. This should match the card ID from the Excel source.")]
        public int cardId;

        [Tooltip("Card display name.")]
        public string cardName;

        [Tooltip("Card description text.")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Card type: Tenant, Equipment, Event, or Contract.")]
        public CardType cardType;

        [Tooltip("Card rarity.")]
        public CardRarity rarity;

        [Tooltip("Card artwork.")]
        public Sprite cardArt;

        [Tooltip("Play cost.")]
        public int cost;

        [Tooltip("Base rent. Only used by Tenant cards.")]
        public int baseRent;

        [Header("Play Target")]
        [Tooltip("Tenant and Equipment cards always use Room. Event and Contract cards read this field.")]
        public CardPlayTargetKind targetKind = CardPlayTargetKind.PlayArea;

        [Header("Effect Configuration")]
        [Tooltip("Prepare phase effect. Example: AddMoney;100")]
        public string preEffect;

        [Tooltip("Instant effect, resolved when the card is played.")]
        public string instantEffect;

        [Tooltip("Settle effect, triggered during the settle phase.")]
        public string settleEffect;

        [Tooltip("Destroy effect, triggered when the card is destroyed.")]
        public string destroyEffect;

        [Header("Persistent Attributes")]
        [Tooltip("Durability. Usually decreases during settle. 0 means unlimited.")]
        public int durability;

        [Tooltip("Wait turns. Decreases by 1 at the end of each turn. 0 means no waiting.")]
        public int waitTurns;
    }
}

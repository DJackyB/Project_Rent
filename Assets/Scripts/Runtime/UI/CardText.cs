using BaoZuPo.Card;
using Martian.Localization;

namespace BaoZuPo.UI
{
    /// <summary>
    /// Localized accessors for card-facing text that preserve CardData as the fallback source.
    /// </summary>
    public static class CardText
    {
        private const string Table = "Card";

        public static string Name(CardInstance card)
        {
            return Name(card != null ? card.Data : null);
        }

        public static string Name(CardData data)
        {
            return Resolve(data, "Name", data != null ? data.cardName : string.Empty);
        }

        public static string Description(CardInstance card)
        {
            return Description(card != null ? card.Data : null);
        }

        public static string Description(CardData data)
        {
            return Resolve(data, "Description", data != null ? data.description : string.Empty);
        }

        private static string Resolve(CardData data, string field, string fallback)
        {
            if (data == null)
            {
                return string.Empty;
            }

            return LocalizationServices.Resolve(
                new LocalizationTextRef(
                    Table,
                    $"Card.{data.cardId}.{field}",
                    fallback ?? string.Empty));
        }
    }
}

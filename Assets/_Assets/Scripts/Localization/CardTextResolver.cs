using BaoZuPo.Card;
using Martian.Localization;

namespace BaoZuPo.Localization
{
    public static class CardTextResolver
    {
        public static string ResolveName(CardData data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            return LocalizationServices.Resolve(new LocalizationTextRef(
                BaoZuPoLocalizationConstants.CardsTable,
                data.ResolveNameTextKey(),
                data.DefaultName));
        }

        public static string ResolveDescription(CardData data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            return LocalizationServices.Resolve(new LocalizationTextRef(
                BaoZuPoLocalizationConstants.CardsTable,
                data.ResolveDescriptionTextKey(),
                data.DefaultDescription));
        }

        public static string ResolveName(CardInstance card)
        {
            return card != null ? ResolveName(card.Data) : string.Empty;
        }

        public static string ResolveDescription(CardInstance card)
        {
            return card != null ? ResolveDescription(card.Data) : string.Empty;
        }
    }
}

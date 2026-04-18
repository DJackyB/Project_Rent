namespace BaoZuPo.Editor
{
    /// <summary>
    /// Code-defined library metadata. Membership and quantity come from the
    /// CardLibrary sheet (Sheet index 1) in CardData.xlsx; this file only owns
    /// stable ids, asset names, display names, and special rules.
    /// </summary>
    internal static class CardSheetCatalog
    {
        /// <summary>卡牌数据所在 Sheet 的索引（Sheet 0）。</summary>
        internal const int CardSheetIndex = 0;

        /// <summary>卡牌库配置所在 Sheet 的索引（Sheet 1）。</summary>
        internal const int LibrarySheetIndex = 1;

        internal const string AllCardsLibraryId = "AllCards";
        internal const string FirstTurnLibraryId = "0";
        internal const string NormalTurnLibraryId = "1";
        internal const string RewardLibraryId = "2";
        internal const string ShopLibraryId = "3";

        internal sealed class LibraryRow
        {
            public string AssetName { get; set; }
            public string LibraryId { get; set; }
            public string DisplayName { get; set; }
            public bool IncludeAllCards { get; set; }
        }

        internal static readonly LibraryRow[] Libraries =
        {
            L("AllCards",      AllCardsLibraryId,   "All Cards",        includeAllCards: true),
            L("FirstTurnPool", FirstTurnLibraryId,  "First Turn Pool"),
            L("NormalTurnPool",NormalTurnLibraryId, "Normal Turn Pool"),
            L("RewardPool",    RewardLibraryId,     "Reward Pool"),
            L("ShopPool",      ShopLibraryId,       "Shop Pool"),
        };

        private static LibraryRow L(string assetName, string libraryId, string displayName, bool includeAllCards = false)
        {
            return new LibraryRow
            {
                AssetName = assetName,
                LibraryId = libraryId,
                DisplayName = displayName,
                IncludeAllCards = includeAllCards,
            };
        }
    }
}

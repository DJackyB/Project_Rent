namespace BaoZuPo.Editor
{
    /// <summary>
    /// Code-defined library metadata. Membership comes from Excel so this file
    /// only owns stable ids, asset names, display names, and special rules.
    /// </summary>
    internal static class CardSheetCatalog
    {
        internal const string AllCardsLibraryId = "AllCards";
        internal const string FirstTurnLibraryId = "0";
        internal const string NormalTurnLibraryId = "1";
        internal const string RewardLibraryId = "2";

        internal sealed class LibraryRow
        {
            public string AssetName { get; set; }
            public string LibraryId { get; set; }
            public string DisplayName { get; set; }
            public bool IncludeAllCards { get; set; }
        }

        internal static readonly LibraryRow[] Libraries =
        {
            L("AllCards", AllCardsLibraryId, "All Cards", includeAllCards: true),
            L("FirstTurnPool", FirstTurnLibraryId, "First Turn Pool"),
            L("NormalTurnPool", NormalTurnLibraryId, "Normal Turn Pool"),
            L("RewardPool", RewardLibraryId, "Reward Pool"),
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

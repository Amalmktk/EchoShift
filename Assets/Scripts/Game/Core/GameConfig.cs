namespace EchoShift.Game
{
    /// <summary>
    /// Values to change before release. Everything else reads from here.
    /// </summary>
    public static class GameConfig
    {
        // Decision D4: replace once the studio and game names are cleared.
        public const string StudioName = "Your Studio";
        public const string GameName = "Echo Shift";
        public const string PackageId = "com.yourstudio.echoshift";

        public const string PrivacyPolicyUrl = "https://example.com/privacy";
        public const string TermsUrl = "https://example.com/terms";

        // In-app products. Create the same ids in Google Play Console.
        public const string ProductRemoveAds = "remove_ads";
        public const string ProductHints10 = "hints_10";
        public const string ProductHints30 = "hints_30";

        // AdMob. These are Google's public test ad units. Swap for live ids in release builds only.
        public const string AdMobRewardedId = "ca-app-pub-3940256099942544/5224354917";
        public const string AdMobInterstitialId = "ca-app-pub-3940256099942544/1033173712";

        // Hint economy (chapter 3 of the project docs).
        public const int StartingHints = 3;
        public const int DailyFreeHints = 1;

        // Interstitial rules.
        public const int NoInterstitialsUpToLevel = 5;
        public const int MinLevelsBetweenInterstitials = 3;
        public const float MinSecondsBetweenInterstitials = 180f;
        public const int MaxInterstitialsPerSession = 6;
        public const int OfferRemoveAdsAfterInterstitials = 3;

        // In-app review.
        public const int AskReviewAfterLevels = 5;
        public const int DaysBetweenReviewAsks = 30;

        public static readonly string[] WorldNames =
        {
            "First Signal", "Sky Relay", "Ember Vault", "Frozen Memory", "The Rift"
        };
    }
}

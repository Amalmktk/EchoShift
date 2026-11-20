# Adding the SDKs

Echo Shift compiles and runs with no third-party SDKs. Each SDK has a ready implementation in the code, switched off until you add the SDK and turn on its scripting define.

To add a define, open **Project Settings > Player > Android > Other Settings > Scripting Define Symbols** and add the name, separated by semicolons.

| SDK | Define | Code |
|---|---|---|
| Google Mobile Ads (AdMob), with consent | `ECHO_ADMOB` | `Services/AdsService.cs` |
| Unity IAP with Google Play Billing | `ECHO_IAP` | `Services/StoreService.cs` |
| Firebase Analytics | `ECHO_FIREBASE` | `Services/AnalyticsService.cs` |
| Firebase Crashlytics, on top of Analytics | `ECHO_CRASHLYTICS` | `Services/AnalyticsService.cs` |
| Google Play In-App Review | `ECHO_PLAY_REVIEW` | `Services/ReviewService.cs` |

Add one SDK at a time, then build and test on a phone before adding the next. The SDK code paths have not been compiled against the real SDKs yet, so expect small fixes the first time each one is switched on.

## AdMob

1. Import the Google Mobile Ads Unity plugin, version 9 or later, from Google's GitHub releases or OpenUPM.
2. Open **Assets > Google Mobile Ads > Settings** and enter your AdMob **Android app ID**.
3. Add `ECHO_ADMOB`.
4. Keep Google's test ad unit ids in `GameConfig.cs` during development. Switch to your live ids only in release builds, and never tap your own live ads.
5. Set up the consent message (GDPR) in the AdMob console under **Privacy & messaging**. The game shows it on first launch and from **Settings > Privacy options**.

## Unity IAP

1. In **Window > Package Manager**, install **In-App Purchasing** version 4.12 or a later 4.x release. Version 5 changed its API.
2. Add `ECHO_IAP`.
3. In Play Console, create the products `remove_ads` as a one-time product, and `hints_10` and `hints_30`, which can be bought repeatedly.
4. Set the prices from chapter 3 of the project docs. Set the Indian prices by hand at ₹199, ₹49 and ₹99.
5. Test with licence testers on the internal testing track.

## Firebase

1. Create a Firebase project and add an Android app with the package id from `GameConfig.cs`.
2. Download `google-services.json` and put it anywhere under `Assets/`.
3. Import **FirebaseAnalytics** from the Firebase Unity SDK and add `ECHO_FIREBASE`.
4. For crash reports, also import **FirebaseCrashlytics** and add `ECHO_CRASHLYTICS`.

## Play In-App Review

1. Import the **Google Play In-App Review** package from Google's Play Unity Plugins.
2. Add `ECHO_PLAY_REVIEW`.
3. The game asks after the 5th completed level, at most once a month. Google decides whether the prompt actually appears.

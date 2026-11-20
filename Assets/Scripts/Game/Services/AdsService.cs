using System;
using UnityEngine;
#if ECHO_ADMOB
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace EchoShift.Game
{
    public interface IAdsProvider
    {
        void RequestConsent(Action done);
        void Initialize(Action ready);
        bool PrivacyOptionsRequired { get; }
        void ShowPrivacyOptions(Action done);
        bool IsRewardedReady { get; }
        void ShowRewarded(Action<bool> done);
        bool IsInterstitialReady { get; }
        void ShowInterstitial(Action done);
    }

    /// <summary>
    /// Ad rules from chapter 3: rewarded ads are always opt-in; interstitials only after
    /// tapping Next, never in the first five levels, spaced out, capped, and removed by purchase.
    /// </summary>
    public class AdsService
    {
        readonly IAdsProvider _provider;
        readonly SaveSystem _save;
        readonly AnalyticsService _analytics;

        int _levelsSinceInterstitial;
        float _lastInterstitialTime = -9999f;
        int _sessionInterstitials;

        public AdsService(SaveSystem save, AnalyticsService analytics)
        {
            _save = save;
            _analytics = analytics;
#if ECHO_ADMOB
            _provider = new AdMobProvider();
#else
            _provider = new TestAdsProvider();
#endif
        }

        /// <summary>True when no real ad SDK is compiled in. Rewarded ads then succeed instantly in test builds.</summary>
        public static bool IsTestProvider
        {
            get
            {
#if ECHO_ADMOB
                return false;
#else
                return true;
#endif
            }
        }

        public void Start(Action ready)
        {
            _provider.RequestConsent(() => _provider.Initialize(ready));
        }

        public bool PrivacyOptionsRequired => _provider.PrivacyOptionsRequired;
        public void ShowPrivacyOptions(Action done) => _provider.ShowPrivacyOptions(done);

        public bool IsRewardedReady => _provider.IsRewardedReady;

        public void ShowRewarded(string placement, Action<bool> done)
        {
            if (!_provider.IsRewardedReady)
            {
                _analytics.Log("ad_rewarded", "placement", placement, "result", "unavailable");
                done(false);
                return;
            }
            _provider.ShowRewarded(earned =>
            {
                _analytics.Log("ad_rewarded", "placement", placement, "result", earned ? "completed" : "skipped");
                done(earned);
            });
        }

        public void NoteLevelCompleted() => _levelsSinceInterstitial++;

        /// <summary>Called when the player taps Next. Shows an interstitial only if every rule allows it.</summary>
        public void MaybeShowInterstitial(int completedLevelNumber, Action then)
        {
            bool allowed =
                !_save.Data.removeAds &&
                completedLevelNumber > GameConfig.NoInterstitialsUpToLevel &&
                _levelsSinceInterstitial >= GameConfig.MinLevelsBetweenInterstitials &&
                Time.realtimeSinceStartup - _lastInterstitialTime >= GameConfig.MinSecondsBetweenInterstitials &&
                _sessionInterstitials < GameConfig.MaxInterstitialsPerSession &&
                _provider.IsInterstitialReady;

            if (!allowed)
            {
                then();
                return;
            }

            _levelsSinceInterstitial = 0;
            _lastInterstitialTime = Time.realtimeSinceStartup;
            _sessionInterstitials++;
            _save.Data.interstitialsShown++;
            _save.Save();
            _analytics.Log("ad_interstitial", "after_level", completedLevelNumber);
            _provider.ShowInterstitial(then);
        }

        /// <summary>True once, after the third interstitial, to offer Remove ads.</summary>
        public bool ShouldOfferRemoveAds()
        {
            if (_save.Data.removeAds || _save.Data.removeAdsOfferShown) return false;
            if (_save.Data.interstitialsShown < GameConfig.OfferRemoveAdsAfterInterstitials) return false;
            _save.Data.removeAdsOfferShown = true;
            _save.Save();
            return true;
        }
    }

    /// <summary>
    /// Used until the AdMob SDK is added. In the editor and development builds rewarded ads
    /// succeed instantly so every flow can be tested; release builds report "not available".
    /// </summary>
    public class TestAdsProvider : IAdsProvider
    {
        static bool TestBuild => Application.isEditor || Debug.isDebugBuild;

        public void RequestConsent(Action done) => done();
        public void Initialize(Action ready) => ready?.Invoke();
        public bool PrivacyOptionsRequired => false;
        public void ShowPrivacyOptions(Action done) => done();
        public bool IsRewardedReady => TestBuild;

        public void ShowRewarded(Action<bool> done)
        {
            Debug.Log("Echo Shift: test rewarded ad shown (no ad SDK installed)");
            done(TestBuild);
        }

        public bool IsInterstitialReady => TestBuild;

        public void ShowInterstitial(Action done)
        {
            Debug.Log("Echo Shift: test interstitial shown (no ad SDK installed)");
            done();
        }
    }

#if ECHO_ADMOB
    /// <summary>Google Mobile Ads (AdMob) with Google's User Messaging Platform for consent.</summary>
    public class AdMobProvider : IAdsProvider
    {
        RewardedAd _rewarded;
        InterstitialAd _interstitial;
        bool _initialized;

        public void RequestConsent(Action done)
        {
            var request = new ConsentRequestParameters();
            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null)
                {
                    Debug.LogWarning("Echo Shift: consent update failed: " + updateError.Message);
                    done();
                    return;
                }
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null) Debug.LogWarning("Echo Shift: consent form failed: " + formError.Message);
                    done();
                });
            });
        }

        public void Initialize(Action ready)
        {
            if (_initialized || !ConsentInformation.CanRequestAds())
            {
                ready?.Invoke();
                return;
            }
            _initialized = true;
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.Initialize(status =>
            {
                LoadRewarded();
                LoadInterstitial();
                ready?.Invoke();
            });
        }

        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public void ShowPrivacyOptions(Action done)
        {
            ConsentForm.ShowPrivacyOptionsForm(error => done());
        }

        void LoadRewarded()
        {
            RewardedAd.Load(GameConfig.AdMobRewardedId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null) return;
                _rewarded = ad;
            });
        }

        void LoadInterstitial()
        {
            InterstitialAd.Load(GameConfig.AdMobInterstitialId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null) return;
                _interstitial = ad;
            });
        }

        public bool IsRewardedReady => _rewarded != null && _rewarded.CanShowAd();

        public void ShowRewarded(Action<bool> done)
        {
            if (!IsRewardedReady)
            {
                done(false);
                return;
            }
            var ad = _rewarded;
            _rewarded = null;
            bool earned = false;
            ad.OnAdFullScreenContentClosed += () =>
            {
                ad.Destroy();
                LoadRewarded();
                done(earned);
            };
            ad.OnAdFullScreenContentFailed += error =>
            {
                ad.Destroy();
                LoadRewarded();
                done(false);
            };
            ad.Show(reward => earned = true);
        }

        public bool IsInterstitialReady => _interstitial != null && _interstitial.CanShowAd();

        public void ShowInterstitial(Action done)
        {
            if (!IsInterstitialReady)
            {
                done();
                return;
            }
            var ad = _interstitial;
            _interstitial = null;
            ad.OnAdFullScreenContentClosed += () =>
            {
                ad.Destroy();
                LoadInterstitial();
                done();
            };
            ad.OnAdFullScreenContentFailed += error =>
            {
                ad.Destroy();
                LoadInterstitial();
                done();
            };
            ad.Show();
        }
    }
#endif
}

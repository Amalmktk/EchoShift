using System;
using UnityEngine;
#if ECHO_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace EchoShift.Game
{
    public interface IStoreProvider
    {
        /// <param name="onGrant">Called with a product id whenever a purchase or restore must be granted.</param>
        void Initialize(Action<string> onGrant);
        bool IsReady { get; }
        string Price(string productId);
        void Buy(string productId, Action<bool> done);
        void Restore(Action<bool> done);
    }

    /// <summary>Remove ads and hint packs. Grants are applied here so every provider behaves the same.</summary>
    public class StoreService
    {
        readonly IStoreProvider _provider;
        readonly SaveSystem _save;
        readonly AnalyticsService _analytics;

        public event Action<string> Granted;

        public StoreService(SaveSystem save, AnalyticsService analytics)
        {
            _save = save;
            _analytics = analytics;
#if ECHO_IAP
            _provider = new UnityIapStore();
#else
            _provider = new TestStoreProvider();
#endif
        }

        public void Start() => _provider.Initialize(Grant);

        public bool IsReady => _provider.IsReady;
        public bool RemoveAdsOwned => _save.Data.removeAds;
        public string Price(string id) => _provider.Price(id);

        public void Buy(string productId, Action<bool> done)
        {
            _provider.Buy(productId, ok =>
            {
                _analytics.Log("purchase", "product_id", productId, "result", ok ? "success" : "failed");
                done(ok);
            });
        }

        public void Restore(Action<bool> done) => _provider.Restore(done);

        void Grant(string productId)
        {
            switch (productId)
            {
                case GameConfig.ProductRemoveAds:
                    _save.Data.removeAds = true;
                    break;
                case GameConfig.ProductHints10:
                    _save.Data.hints += 10;
                    break;
                case GameConfig.ProductHints30:
                    _save.Data.hints += 30;
                    break;
                default:
                    Debug.LogWarning("Echo Shift: unknown product " + productId);
                    return;
            }
            _save.Save();
            Granted?.Invoke(productId);
        }
    }

    /// <summary>
    /// Used until Unity IAP is added. Purchases succeed instantly in the editor and development
    /// builds so the Shop can be tested; release builds report that the store is unavailable.
    /// </summary>
    public class TestStoreProvider : IStoreProvider
    {
        Action<string> _grant;
        static bool TestBuild => Application.isEditor || Debug.isDebugBuild;

        public void Initialize(Action<string> onGrant) => _grant = onGrant;
        public bool IsReady => TestBuild;

        public string Price(string productId)
        {
            switch (productId)
            {
                case GameConfig.ProductRemoveAds: return "$2.99";
                case GameConfig.ProductHints10: return "$0.99";
                case GameConfig.ProductHints30: return "$1.99";
                default: return "";
            }
        }

        public void Buy(string productId, Action<bool> done)
        {
            if (!TestBuild)
            {
                done(false);
                return;
            }
            Debug.Log("Echo Shift: test purchase of " + productId + " (no store SDK installed)");
            _grant(productId);
            done(true);
        }

        public void Restore(Action<bool> done) => done(TestBuild);
    }

#if ECHO_IAP
    /// <summary>Unity IAP 4.x with Google Play Billing.</summary>
    public class UnityIapStore : IStoreProvider, IDetailedStoreListener
    {
        IStoreController _controller;
        IExtensionProvider _extensions;
        Action<string> _grant;
        Action<bool> _pending;

        public void Initialize(Action<string> onGrant)
        {
            _grant = onGrant;
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.AddProduct(GameConfig.ProductRemoveAds, ProductType.NonConsumable);
            builder.AddProduct(GameConfig.ProductHints10, ProductType.Consumable);
            builder.AddProduct(GameConfig.ProductHints30, ProductType.Consumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public bool IsReady => _controller != null;

        public string Price(string productId)
        {
            var p = _controller?.products.WithID(productId);
            return p != null && p.availableToPurchase ? p.metadata.localizedPriceString : "";
        }

        public void Buy(string productId, Action<bool> done)
        {
            if (_controller == null)
            {
                done(false);
                return;
            }
            _pending = done;
            _controller.InitiatePurchase(productId);
        }

        public void Restore(Action<bool> done)
        {
            if (_extensions == null)
            {
                done(false);
                return;
            }
            // Google Play restores owned non-consumables through ProcessPurchase.
            _extensions.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions((ok, message) => done(ok));
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            var removeAds = controller.products.WithID(GameConfig.ProductRemoveAds);
            if (removeAds != null && removeAds.hasReceipt) _grant(GameConfig.ProductRemoveAds);
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogWarning("Echo Shift: store init failed: " + error);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogWarning("Echo Shift: store init failed: " + error + " " + message);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            _grant(args.purchasedProduct.definition.id);
            _pending?.Invoke(true);
            _pending = null;
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            _pending?.Invoke(false);
            _pending = null;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription description)
        {
            _pending?.Invoke(false);
            _pending = null;
        }
    }
#endif
}

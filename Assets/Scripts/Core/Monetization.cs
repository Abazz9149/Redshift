using System;
using UnityEngine;

namespace Redshift
{
    /// <summary>
    /// Ads facade. Replace the TODO bodies with Unity LevelPlay / Google AdMob calls.
    /// Rewarded ads: CONTINUE after crash, DOUBLE COINS on results.
    /// Interstitials: every Nth run, never for Remove Ads / Premium owners.
    /// </summary>
    public static class Ads
    {
        public static int InterstitialEveryRuns = 3;

        public static bool AdsRemoved => SaveSystem.Data.removeAds || SaveSystem.Data.premium;

        public static void ShowRewarded(string placement, Action<bool> onDone)
        {
            // TODO: show rewarded ad, call onDone(true) only when the reward callback fires.
            Debug.Log($"[Ads] (stub) rewarded '{placement}' granted");
            onDone?.Invoke(true);
        }

        /// <summary>Call when the player leaves a results screen.</summary>
        public static void MaybeShowInterstitial()
        {
            if (AdsRemoved) return;
            var d = SaveSystem.Data;
            d.runsSinceInterstitial++;
            if (d.runsSinceInterstitial < InterstitialEveryRuns) return;
            d.runsSinceInterstitial = 0;
            // TODO: show interstitial
            Debug.Log("[Ads] (stub) interstitial");
        }
    }

    /// <summary>
    /// In-app purchases. Product ids must match Google Play Console.
    /// Replace Purchase() with Unity IAP (com.unity.purchasing) and call Grant() from ProcessPurchase.
    /// </summary>
    public static class Store
    {
        public const string RemoveAds = "remove_ads";
        public const string Premium = "premium_vip";
        public const string DiamondsSmall = "diamonds_100";
        public const string DiamondsLarge = "diamonds_600";
        public const string StarterPack = "starter_pack";

        public static event Action<string> OnPurchased;

        public static bool Owns(string productId)
        {
            var d = SaveSystem.Data;
            if (productId == RemoveAds) return d.removeAds || d.premium;
            if (productId == Premium) return d.premium;
            return false;
        }

        public static void Purchase(string productId, Action<bool> onDone)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Store] (stub) purchased {productId}");
            Grant(productId);
            onDone?.Invoke(true);
#else
            // TODO: route through Unity IAP. The stub never grants items in release builds.
            Debug.LogError("[Store] IAP not integrated yet");
            onDone?.Invoke(false);
#endif
        }

        /// <summary>Gives the content of a product. Called after a verified purchase or restore.</summary>
        public static void Grant(string productId)
        {
            var d = SaveSystem.Data;
            switch (productId)
            {
                case RemoveAds: d.removeAds = true; break;
                case Premium:
                    d.premium = true;
                    d.removeAds = true;
                    var vip = GameManager.I != null ? GameManager.I.premiumCar : null;
                    if (vip != null && !d.ownedCars.Contains(vip.id)) d.ownedCars.Add(vip.id);
                    break;
                case DiamondsSmall: Economy.Add(Currency.Diamonds, 100); break;
                case DiamondsLarge: Economy.Add(Currency.Diamonds, 600); break;
                case StarterPack:
                    Economy.Add(Currency.Credits, 5000);
                    Economy.Add(Currency.Diamonds, 50);
                    d.removeAds = true;
                    break;
            }
            SaveSystem.Save();
            Economy.NotifyChanged();
            OnPurchased?.Invoke(productId);
        }
    }
}

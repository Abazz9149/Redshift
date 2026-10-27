using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>
    /// STORE (Frame 8). Each offer = one product id from Store (remove_ads, diamonds_100, starter_pack...).
    /// Price labels should come from Unity IAP's localized price once integrated (shows ₹ correctly).
    /// </summary>
    public class StoreUI : MonoBehaviour
    {
        [Serializable]
        public class Offer
        {
            public string productId;
            public Button buyButton;
            public TMP_Text priceText;
            public string fallbackPrice = "₹99";
            public GameObject ownedState;      // for one-time items like Remove Ads
        }

        public Offer[] offers;
        public Button backButton;
        public ScreenRouter router;

        void Awake()
        {
            foreach (var o in offers)
            {
                var offer = o;
                offer.buyButton.onClick.AddListener(() => Buy(offer));
            }
            if (backButton) backButton.onClick.AddListener(() => router.Back());
        }

        void OnEnable()
        {
            Store.OnPurchased += OnPurchased;
            Refresh();
        }

        void OnDisable() => Store.OnPurchased -= OnPurchased;

        void OnPurchased(string id) => Refresh();

        void Buy(Offer o)
        {
            o.buyButton.interactable = false;
            Store.Purchase(o.productId, ok => Refresh());
        }

        void Refresh()
        {
            foreach (var o in offers)
            {
                bool owned = Store.Owns(o.productId);
                o.buyButton.interactable = !owned;
                if (o.ownedState) o.ownedState.SetActive(owned);
                if (o.priceText) o.priceText.text = owned ? "OWNED" : o.fallbackPrice;
            }
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>
    /// PREMIUM / VIP (Frame 9). Every listed benefit is implemented in the kit:
    ///  2x coins (RaceSession payout + mission rewards), remove ads (Ads.AdsRemoved),
    ///  free continue (RaceSession.ContinueIsFree), exclusive car (GameManager.premiumCar, granted in Store.Grant).
    /// </summary>
    public class PremiumUI : MonoBehaviour
    {
        public Button buyButton;
        public TMP_Text priceText;
        public string fallbackPrice = "₹399";
        public GameObject ownedState;          // "ACTIVE" badge
        public Button closeButton;
        public ScreenRouter router;
        public Showroom showroom;              // optional: preview the VIP car

        void Awake()
        {
            buyButton.onClick.AddListener(Buy);
            if (closeButton) closeButton.onClick.AddListener(() => router.Back());
        }

        void OnEnable()
        {
            Refresh();
            var vip = GameManager.I != null ? GameManager.I.premiumCar : null;
            if (showroom && vip) showroom.Display(vip);
        }

        void OnDisable()
        {
            if (showroom && GameManager.I != null) showroom.Display(GameManager.I.SelectedCar);
        }

        void Buy()
        {
            buyButton.interactable = false;
            Store.Purchase(Store.Premium, ok => Refresh());
        }

        void Refresh()
        {
            bool owned = SaveSystem.Data.premium;
            buyButton.interactable = !owned;
            if (ownedState) ownedState.SetActive(owned);
            if (priceText) priceText.text = owned ? "ACTIVE" : fallbackPrice;
        }
    }
}

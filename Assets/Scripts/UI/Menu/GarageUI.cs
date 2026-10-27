using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>
    /// GARAGE (Frame 10): PREVIOUS / NEXT through cars, stat bars, BUY / SELECT, upgrades.
    /// The header bars (POWER / DRAG / STEERING in the Figma) map to Top speed / Acceleration / Handling / Nitro here.
    /// </summary>
    public class GarageUI : MonoBehaviour
    {
        [Serializable]
        public class UpgradeRow
        {
            public UpgradeType type;
            public Button button;
            public TMP_Text costText;
            public TMP_Text levelText;         // "LV 2/5"
        }

        public Showroom showroom;
        public ScreenRouter router;

        [Header("Car info")]
        public TMP_Text nameText;
        public TMP_Text classText;
        public TMP_Text topSpeedText;          // "420 KM/H"
        public Image topSpeedBar;
        public Image accelerationBar;
        public Image handlingBar;
        public Image nitroBar;

        [Header("Actions")]
        public Button previousButton;
        public Button nextButton;
        public Button buyButton;
        public TMP_Text buyPriceText;
        public GameObject creditIcon;
        public GameObject diamondIcon;
        public Button selectButton;
        public GameObject selectedBadge;
        public GameObject lockedGroup;
        public TMP_Text lockedText;            // "REACH LV 10"
        public UpgradeRow[] upgrades;
        public Button backButton;

        [Header("Feedback")]
        public GameObject notEnoughFunds;      // small toast, auto-hides

        CarDatabase db;
        int index;
        CarStats max;

        void Awake()
        {
            previousButton.onClick.AddListener(() => Step(-1));
            nextButton.onClick.AddListener(() => Step(1));
            buyButton.onClick.AddListener(Buy);
            selectButton.onClick.AddListener(() => { Garage.Select(Current); Refresh(); });
            foreach (var u in upgrades)
            {
                var row = u;
                row.button.onClick.AddListener(() => DoUpgrade(row.type));
            }
            if (backButton) backButton.onClick.AddListener(() => router.Back());
        }

        void OnEnable()
        {
            db = GameManager.I.cars;
            max = db.MaxStats();
            index = db.IndexOf(SaveSystem.Data.selectedCarId);
            Economy.OnChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            Economy.OnChanged -= Refresh;
            // Leaving the garage: show the selected car again on HOME
            if (showroom && GameManager.I != null) showroom.Display(GameManager.I.SelectedCar);
        }

        CarData Current => db.cars[index];

        void Step(int dir)
        {
            index = (index + dir + db.cars.Count) % db.cars.Count;
            Refresh();
        }

        void Refresh()
        {
            if (db == null || db.cars.Count == 0) return;
            var car = Current;
            bool owned = Garage.Owns(car);
            bool levelLocked = !owned && Garage.IsLevelLocked(car);
            var s = Garage.Stats(car);

            if (showroom) showroom.Display(car);
            if (nameText) nameText.text = car.displayName;
            if (classText) classText.text = $"CLASS {car.carClass}";
            if (topSpeedText) topSpeedText.text = $"{Mathf.RoundToInt(s.topSpeedKmh)} KM/H";
            SetBar(topSpeedBar, s.topSpeedKmh, max.topSpeedKmh);
            SetBar(accelerationBar, s.acceleration, max.acceleration);
            SetBar(handlingBar, s.handling, max.handling);
            SetBar(nitroBar, s.nitroSeconds, max.nitroSeconds);

            buyButton.gameObject.SetActive(!owned && !levelLocked);
            if (buyPriceText) buyPriceText.text = car.price.ToString("N0");
            if (creditIcon) creditIcon.SetActive(car.priceCurrency == Currency.Credits);
            if (diamondIcon) diamondIcon.SetActive(car.priceCurrency == Currency.Diamonds);
            buyButton.interactable = Economy.CanAfford(car.priceCurrency, car.price);

            bool selected = Garage.IsSelected(car);
            selectButton.gameObject.SetActive(owned && !selected);
            if (selectedBadge) selectedBadge.SetActive(selected);
            if (lockedGroup) lockedGroup.SetActive(levelLocked);
            if (lockedText) lockedText.text = $"REACH LV {car.requiredLevel}";

            foreach (var u in upgrades)
            {
                bool maxed = Garage.IsMaxed(car, u.type);
                int cost = Garage.UpgradeCost(car, u.type);
                u.button.gameObject.SetActive(owned);
                u.button.interactable = !maxed && Economy.CanAfford(Currency.Credits, cost);
                if (u.costText) u.costText.text = maxed ? "MAX" : cost.ToString("N0");
                if (u.levelText) u.levelText.text = $"LV {Garage.Level(car, u.type)}/{car.maxUpgradeLevel}";
            }
        }

        static void SetBar(Image bar, float v, float maxV)
        {
            if (bar) bar.fillAmount = maxV > 0f ? Mathf.Clamp01(v / maxV) : 0f;
        }

        void Buy()
        {
            if (Garage.Buy(Current)) { Garage.Select(Current); Refresh(); }
            else ShowNoFunds();
        }

        void DoUpgrade(UpgradeType t)
        {
            if (!Garage.Upgrade(Current, t)) ShowNoFunds();
            Refresh();
        }

        void ShowNoFunds()
        {
            if (!notEnoughFunds) return;
            notEnoughFunds.SetActive(true);
            CancelInvoke(nameof(HideNoFunds));
            Invoke(nameof(HideNoFunds), 1.5f);
        }

        void HideNoFunds() { if (notEnoughFunds) notEnoughFunds.SetActive(false); }
    }
}

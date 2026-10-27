using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>HOME (Frame 1): READY TO RACE, selected car name, PLAY (last mode), side buttons.</summary>
    public class HomeUI : MonoBehaviour
    {
        public ScreenRouter router;
        public Showroom showroom;
        public TMP_Text carNameText;
        public TMP_Text playModeText;          // small text under PLAY: "ENDLESS"
        public Button playButton;              // opens SELECT GAME MODE (its PLAY starts the race)
        public Button modeButton;              // optional: opens mode select

        void Awake()
        {
            playButton.onClick.AddListener(() => router.Open("modes"));
            if (modeButton) modeButton.onClick.AddListener(() => router.Open("modes"));
        }

        void OnEnable()
        {
            Garage.OnChanged += Refresh;
            Refresh();
        }

        void OnDisable() => Garage.OnChanged -= Refresh;

        void Refresh()
        {
            var car = GameManager.I != null ? GameManager.I.SelectedCar : null;
            if (car != null)
            {
                if (carNameText) carNameText.text = car.displayName;
                if (showroom) showroom.Display(car);
            }
            if (playModeText && GameManager.I != null) playModeText.text = ModeSelectUI.ModeName(GameManager.I.SelectedMode);
        }
    }
}

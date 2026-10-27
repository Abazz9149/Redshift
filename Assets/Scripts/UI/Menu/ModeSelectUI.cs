using System;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>
    /// SELECT GAME MODE (Frame 3). Launch with ENDLESS, TIME TRIAL, TWO-WAY.
    /// Career / Multiplayer cards can stay in the layout with lockedOverlay = "COMING SOON".
    /// </summary>
    public class ModeSelectUI : MonoBehaviour
    {
        [Serializable]
        public class ModeCard
        {
            public GameMode mode;
            public Button button;
            public GameObject selectedState;   // red "[ SELECTED ] READY" strip
            public GameObject normalState;     // grey "[ SELECT ] →"
        }

        public ModeCard[] cards;
        public Button playButton;
        public Button backButton;
        public ScreenRouter router;

        void Awake()
        {
            foreach (var c in cards)
            {
                var card = c;
                card.button.onClick.AddListener(() => Select(card.mode));
            }
            playButton.onClick.AddListener(() => SceneLoader.ToRace());
            if (backButton) backButton.onClick.AddListener(() => router.Back());
        }

        void OnEnable() => Refresh();

        void Select(GameMode mode)
        {
            if (GameManager.I != null) GameManager.I.SelectedMode = mode;
            PlayerPrefs.SetInt("lastMode", (int)mode);
            Refresh();
        }

        void Refresh()
        {
            var selected = GameManager.I != null ? GameManager.I.SelectedMode : GameMode.Endless;
            foreach (var c in cards)
            {
                bool on = c.mode == selected;
                if (c.selectedState) c.selectedState.SetActive(on);
                if (c.normalState) c.normalState.SetActive(!on);
            }
        }

        public static string ModeName(GameMode m)
        {
            switch (m)
            {
                case GameMode.TimeTrial: return "TIME TRIAL";
                case GameMode.TwoWay: return "TWO-WAY";
                default: return "ENDLESS";
            }
        }
    }
}

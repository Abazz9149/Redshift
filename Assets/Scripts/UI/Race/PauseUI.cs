using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>
    /// PAUSE (Frame 15): CONTINUE / RESTART / HOME / SETTINGS over the frozen race.
    /// Use a dark translucent full-screen Image behind the buttons, not the garage background.
    /// </summary>
    public class PauseUI : MonoBehaviour
    {
        public RaceSession session;
        public GameObject panel;
        public GameObject settingsPanel;      // reuse the SettingsUI prefab
        public TMP_Text scoreText;
        public TMP_Text distanceText;

        public Button pauseButton;            // the || button in the HUD
        public Button continueButton;
        public Button restartButton;
        public Button homeButton;
        public Button settingsButton;

        void Awake()
        {
            panel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(false);
            pauseButton.onClick.AddListener(() => session.SetPaused(true));
            continueButton.onClick.AddListener(() => session.SetPaused(false));
            restartButton.onClick.AddListener(() => { session.SetPaused(false); SceneLoader.ToRace(); });
            homeButton.onClick.AddListener(() => { session.SetPaused(false); SceneLoader.ToMenu(); });
            if (settingsButton) settingsButton.onClick.AddListener(() => { if (settingsPanel) settingsPanel.SetActive(true); });
        }

        void OnEnable() => session.OnPauseChanged += HandlePause;
        void OnDisable() => session.OnPauseChanged -= HandlePause;

        void HandlePause(bool paused)
        {
            panel.SetActive(paused);
            if (!paused && settingsPanel) settingsPanel.SetActive(false);
            if (!paused) return;
            if (scoreText) scoreText.text = session.Stats.score.ToString("N0");
            if (distanceText) distanceText.text = $"{session.Stats.distanceM / 1000f:0.00} KM";
        }

        void Update()
        {
            if (pauseButton) pauseButton.interactable = session.Running && !session.Paused;
#if ENABLE_LEGACY_INPUT_MANAGER
            // Android back button / Esc toggles pause
            if (Input.GetKeyDown(KeyCode.Escape) && !session.Finished && !session.Crashed)
                session.SetPaused(!session.Paused);
#endif
        }
    }
}

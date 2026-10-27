using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>SETTINGS (Frame 5): MUSIC, SOUND, GRAPHICS (LOW/MEDIUM/HIGH/ULTRA), CONTROLS. Works in menu and pause.</summary>
    public class SettingsUI : MonoBehaviour
    {
        public Slider musicSlider;
        public TMP_Text musicValue;
        public Slider soundSlider;
        public TMP_Text soundValue;

        [Tooltip("4 buttons in order LOW, MEDIUM, HIGH, ULTRA")]
        public Button[] graphicsButtons;
        public Color selectedColor = new Color(1f, 0.16f, 0.32f);
        public Color normalColor = new Color(0.35f, 0.35f, 0.38f);

        [Header("Controls")]
        public Button buttonsModeButton;     // "BUTTONS"
        public Button wheelModeButton;       // "WHEEL"
        public Button tiltModeButton;        // "TILT" (gyro)
        public Slider sensitivitySlider;     // 0.5 .. 1.5 (tilt)

        public Button backButton;
        public Button backToMenuButton;      // only shown in pause
        public ScreenRouter router;          // null when used in the race scene

        void Awake()
        {
            musicSlider.onValueChanged.AddListener(v => { GameSettings.SetMusic(v); Label(musicValue, v); });
            soundSlider.onValueChanged.AddListener(v => { GameSettings.SetSound(v); Label(soundValue, v); });
            for (int i = 0; i < graphicsButtons.Length; i++)
            {
                int level = i;
                graphicsButtons[i].onClick.AddListener(() => { GameSettings.SetGraphics(level); Refresh(); });
            }
            if (buttonsModeButton) buttonsModeButton.onClick.AddListener(() => { GameSettings.SetControlMode(0); Refresh(); });
            if (wheelModeButton) wheelModeButton.onClick.AddListener(() => { GameSettings.SetControlMode(1); Refresh(); });
            if (tiltModeButton) tiltModeButton.onClick.AddListener(() => { GameSettings.SetControlMode(2); Refresh(); });
            if (sensitivitySlider)
                sensitivitySlider.onValueChanged.AddListener(v => { SaveSystem.Data.steerSensitivity = v; SaveSystem.Save(); });
            if (backButton) backButton.onClick.AddListener(Close);
            if (backToMenuButton) backToMenuButton.onClick.AddListener(() => SceneLoader.ToMenu());
        }

        void OnEnable() => Refresh();

        void Refresh()
        {
            var d = SaveSystem.Data;
            musicSlider.SetValueWithoutNotify(d.musicVolume);
            soundSlider.SetValueWithoutNotify(d.soundVolume);
            Label(musicValue, d.musicVolume);
            Label(soundValue, d.soundVolume);
            if (sensitivitySlider) sensitivitySlider.SetValueWithoutNotify(d.steerSensitivity);

            for (int i = 0; i < graphicsButtons.Length; i++)
            {
                var img = graphicsButtons[i].GetComponent<Image>();
                if (img) img.color = i == d.graphicsLevel ? selectedColor : normalColor;
            }
            Tint(buttonsModeButton, d.controlMode == 0);
            Tint(wheelModeButton, d.controlMode == 1);
            Tint(tiltModeButton, d.controlMode == 2);
            if (backToMenuButton) backToMenuButton.gameObject.SetActive(router == null);
        }

        void Tint(Button b, bool on)
        {
            if (!b) return;
            var img = b.GetComponent<Image>();
            if (img) img.color = on ? selectedColor : normalColor;
        }

        static void Label(TMP_Text t, float v) { if (t) t.text = $"{Mathf.RoundToInt(v * 100)}%"; }

        void Close()
        {
            if (router != null) router.Back();
            else gameObject.SetActive(false);
        }
    }
}

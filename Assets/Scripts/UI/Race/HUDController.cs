using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>In-race HUD on your PlayerController: top bar (distance, mission, score, combo, coins, pause),
    /// speedometer, nitro, rival position. Replaces UIManager (which still works if you keep it).</summary>
    public class HUDController : MonoBehaviour
    {
        public RaceSession session;
        public PlayerController player;
        public RivalTracker rivals;             // optional

        [Header("Top bar")]
        public TMP_Text scoreText;
        public TMP_Text distanceText;
        public TMP_Text coinsText;              // credits earned so far this run
        public GameObject comboBadge;
        public TMP_Text comboText;
        public Image comboTimerFill;            // optional radial/bar showing combo time left

        [Header("Time Trial")]
        public GameObject timerGroup;
        public TMP_Text timerText;

        [Header("Mission tracker (optional)")]
        public GameObject missionGroup;
        public TMP_Text missionText;
        public Image missionFill;

        [Header("Rivals (optional)")]
        public GameObject positionGroup;
        public TMP_Text positionText;           // "POS 2/3"

        [Header("Speedometer")]
        public TMP_Text speedText;
        public Image speedFill;                 // Image Type = Filled; full = top speed with nitro
        public Color speedColor = new Color(0f, 0.84f, 1f);
        public Color nitroColor = new Color(1f, 0.28f, 0.28f);

        [Header("Nitro")]
        public Image nitroFill;
        public Button nitroButton;
        public GameObject speedLinesFx;         // full-screen speed lines, enabled during nitro

        [Header("Popups")]
        public NearMissPopup popup;

        MissionManager.Entry trackedMission;
        int missionStartProgress;

        void OnEnable() { if (session) session.OnNearMiss += HandleNearMiss; }
        void OnDisable() { if (session) session.OnNearMiss -= HandleNearMiss; }

        void Start()
        {
            if (timerGroup) timerGroup.SetActive(session.Mode == GameMode.TimeTrial);
            if (positionGroup) positionGroup.SetActive(rivals != null && rivals.Active);

            // Track the first unfinished mission on the HUD
            foreach (var e in MissionManager.Active())
            {
                if (e.Complete || e.def.singleRun) continue;
                trackedMission = e;
                missionStartProgress = e.progress.progress;
                break;
            }
            if (missionGroup) missionGroup.SetActive(trackedMission != null);
        }

        void HandleNearMiss(int points, int combo, bool oncoming)
        {
            if (popup) popup.Show(points, combo, oncoming);
        }

        void Update()
        {
            var s = session.Stats;
            if (scoreText) scoreText.text = s.score.ToString("N0");
            if (distanceText) distanceText.text = $"{s.distanceM / 1000f:0.00} km";
            if (coinsText)
            {
                int coins = s.score / Mathf.Max(1, session.scorePerCoin) * (SaveSystem.Data.premium ? 2 : 1);
                coinsText.text = coins.ToString("N0");
            }

            if (comboBadge) comboBadge.SetActive(session.Combo > 1);
            if (comboText) comboText.text = $"x{session.Combo}";
            if (comboTimerFill) comboTimerFill.fillAmount = session.ComboTimeLeft01;

            if (timerText && session.Mode == GameMode.TimeTrial)
            {
                timerText.text = Mathf.CeilToInt(session.TimeLeft).ToString();
                timerText.color = session.TimeLeft < 10f ? nitroColor : Color.white;
            }

            if (trackedMission != null)
            {
                int live = Mathf.Min(trackedMission.def.target, missionStartProgress + s.Value(trackedMission.def.type));
                if (missionText) missionText.text = $"{trackedMission.def.title}  {trackedMission.def.FormatProgress(live)}";
                if (missionFill) missionFill.fillAmount = live / (float)trackedMission.def.target;
            }

            if (positionText && rivals != null && rivals.Active)
                positionText.text = $"POS {rivals.Position}/{rivals.Total}";

            float kmh = player.currentSpeed;
            if (speedText) speedText.text = Mathf.RoundToInt(kmh).ToString();
            if (speedFill)
            {
                float top = player.maxSpeed * player.nitroSpeedMultiplier;
                speedFill.fillAmount = Mathf.Clamp01(kmh / Mathf.Max(1f, top));
                speedFill.color = player.NitroActive ? nitroColor : speedColor;
            }

            if (nitroFill) nitroFill.fillAmount = player.Nitro01;
            if (nitroButton) nitroButton.interactable = !player.NitroActive && player.Nitro >= player.minNitroToActivate;
            if (speedLinesFx) speedLinesFx.SetActive(player.NitroActive);
        }
    }
}

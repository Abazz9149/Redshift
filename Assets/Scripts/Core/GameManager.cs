using UnityEngine;

namespace Redshift
{
    public enum GameMode { Endless, TimeTrial, TwoWay }

    /// <summary>
    /// Lives across scenes. Put a prefab with this component in Assets/Resources/GameManager.prefab:
    /// it is created automatically before the first scene loads, so you can press Play from any scene.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        [Header("Databases")]
        public CarDatabase cars;
        public MissionDatabase missions;
        public DailyRewardTable dailyRewards;
        [Tooltip("Exclusive car unlocked by Premium / VIP")]
        public CarData premiumCar;

        [Header("Audio")]
        [Tooltip("Mixer with exposed parameters MusicVol and SfxVol (optional)")]
        public UnityEngine.Audio.AudioMixer mixer;

        [Header("Scenes")]
        public string menuScene = "Menu";
        public string raceScene = "Race";

        [Header("Progression")]
        public int xpPerLevel = 1000;

        // Runtime state shared between scenes
        public GameMode SelectedMode { get; set; } = GameMode.Endless;
        public RaceResult LastResult { get; set; }
        /// <summary>Screen id the menu should open on next load (e.g. "garage" after CHANGE CAR).</summary>
        public string PendingMenuScreen { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoCreate()
        {
            if (I != null) return;
            var prefab = Resources.Load<GameManager>("GameManager");
            if (prefab != null) Instantiate(prefab);
            else Debug.LogError("[GameManager] Missing Resources/GameManager prefab.");
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            SaveSystem.Load();
            SelectedMode = (GameMode)Mathf.Clamp(PlayerPrefs.GetInt("lastMode", 0), 0, 2);
            EnsureStarterCar();
            GameSettings.Mixer = mixer;
            GameSettings.ApplyAll();
            MissionManager.Init(missions);
            DailyRewardManager.Init(dailyRewards);
        }

        void EnsureStarterCar()
        {
            var d = SaveSystem.Data;
            if (cars == null || cars.cars.Count == 0) return;
            if (cars.Get(d.selectedCarId) == null) d.selectedCarId = cars.cars[0].id;
            if (!d.ownedCars.Contains(d.selectedCarId)) d.ownedCars.Add(d.selectedCarId);
        }

        public CarData SelectedCar => cars != null ? cars.Get(SaveSystem.Data.selectedCarId) : null;

        public int PlayerLevel => 1 + SaveSystem.Data.xp / Mathf.Max(1, xpPerLevel);
        public float LevelProgress01 => (SaveSystem.Data.xp % Mathf.Max(1, xpPerLevel)) / (float)xpPerLevel;

        void OnApplicationPause(bool paused) { if (paused) SaveSystem.Save(); }
        void OnApplicationQuit() { SaveSystem.Save(); }
    }
}

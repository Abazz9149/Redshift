using System;
using UnityEngine;

namespace Redshift
{
    /// <summary>
    /// One race on top of your PlayerController + RoadManagerRecycle (+ optional AI rivals):
    /// score, near-miss combo, timer, crash / continue and the payout on RACE RESULTS.
    ///
    /// Scoring:
    ///  - distance points (real road distance), up to 2x at top speed
    ///  - near-miss = nearMissPoints x combo (x2 for oncoming cars in Two-Way)
    ///  - combo grows with each near-miss inside comboWindow seconds, max x10
    ///  - at the end: bonus per AI rival you finish ahead of
    /// </summary>
    [DefaultExecutionOrder(-200)]   // Awake applies garage stats before PlayerController/AI Start
    public class RaceSession : MonoBehaviour
    {
        [Header("Your scripts")]
        public PlayerController player;
        public RoadManagerRecycle road;
        public RivalTracker rivals;                // optional
        [Tooltip("Optional: the selected car's prefab is spawned here (child of PlayerController.carBody). Leave empty to keep the car already in the scene.")]
        public Transform carModelMount;

        [Header("Score")]
        public int nearMissPoints = 100;
        public float comboWindow = 3f;
        public int maxCombo = 10;
        public float twoWayScoreMultiplier = 1.5f;
        public float oncomingNearMissMultiplier = 2f;
        public float nitroRefillPerNearMiss = 0.4f;
        public int pointsPerRivalBeaten = 1000;

        [Header("Time Trial")]
        public float timeTrialSeconds = 90f;
        public float timeBonusPerNearMiss = 1.5f;

        [Header("Payout")]
        public int scorePerCoin = 20;
        public int scorePerXp = 10;
        public int maxContinues = 1;
        public float clearRadiusOnContinue = 60f;

        public RunStats Stats { get; private set; } = new RunStats();
        public GameMode Mode { get; private set; }
        public int Combo { get; private set; }
        public float ComboTimeLeft01 => Combo > 0 ? comboTimer / comboWindow : 0f;
        public float TimeLeft { get; private set; }
        public bool Running { get; private set; }
        public bool Paused { get; private set; }
        public bool Crashed { get; private set; }
        public bool Finished { get; private set; }
        public int HighScore => SaveSystem.Data.highScore;

        public bool CanContinue => continuesUsed < maxContinues;
        public bool ContinueIsFree => SaveSystem.Data.premium && !freeContinueUsed;

        /// <summary>points, combo, oncoming</summary>
        public event Action<int, int, bool> OnNearMiss;
        public event Action OnCrashed;
        public event Action OnContinued;
        public event Action<bool> OnPauseChanged;
        public event Action<RaceResult> OnFinished;

        float scoreF;
        float comboTimer;
        int continuesUsed;
        bool freeContinueUsed;

        void Awake()
        {
            Time.timeScale = 1f;
            var gm = GameManager.I;
            Mode = gm != null ? gm.SelectedMode : GameMode.Endless;
            Stats.mode = Mode;

            CarData car = gm != null ? gm.SelectedCar : null;
            if (car != null)
            {
                player.ApplyStats(Garage.Stats(car));
                if (car.prefab != null && carModelMount != null)
                {
                    // Replace the placeholder model with the garage car
                    for (int i = carModelMount.childCount - 1; i >= 0; i--)
                    {
                        var old = carModelMount.GetChild(i).gameObject;
                        old.SetActive(false);   // so bounds/renderers ignore it this frame
                        Destroy(old);
                    }
                    Instantiate(car.prefab, carModelMount, false);
                }
            }

            road.Begin(Mode);
            if (rivals != null) rivals.Setup(Mode, player);
            player.OnNitroStarted += () => Stats.nitroUses++;
            TimeLeft = timeTrialSeconds;
            Running = true;
        }

        void Update()
        {
            if (!Running) return;
            float dt = Time.deltaTime;
            Stats.timeSeconds += dt;

            // Real distance the road moved under the car
            float moved = road.CurrentRoadSpeed * dt;
            Stats.distanceM += moved;
            Stats.topSpeedKmh = Mathf.Max(Stats.topSpeedKmh, player.currentSpeed);

            float range = Mathf.Max(1f, player.maxSpeed - player.minSpeed);
            float speedBonus = 1f + Mathf.Clamp01((player.currentSpeed - player.minSpeed) / range);
            float modeMult = Mode == GameMode.TwoWay ? twoWayScoreMultiplier : 1f;
            scoreF += moved * speedBonus * modeMult;
            Stats.score = Mathf.FloorToInt(scoreF);

            if (Combo > 0)
            {
                comboTimer -= dt;
                if (comboTimer <= 0f) Combo = 0;
            }

            if (Mode == GameMode.TimeTrial)
            {
                TimeLeft -= dt;
                if (TimeLeft <= 0f) { TimeLeft = 0f; Finish(); }
            }
        }

        public void RegisterPass(bool oncoming, bool nearMiss)
        {
            if (!oncoming) Stats.overtakes++;
            if (!nearMiss) return;

            Combo = Mathf.Min(maxCombo, Combo + 1);
            comboTimer = comboWindow;
            Stats.nearMisses++;
            Stats.bestCombo = Mathf.Max(Stats.bestCombo, Combo);

            int pts = Mathf.RoundToInt(nearMissPoints * Combo * (oncoming ? oncomingNearMissMultiplier : 1f));
            scoreF += pts;
            Stats.score = Mathf.FloorToInt(scoreF);

            player.AddNitro(nitroRefillPerNearMiss);
            if (Mode == GameMode.TimeTrial) TimeLeft += timeBonusPerNearMiss;

            OnNearMiss?.Invoke(pts, Combo, oncoming);
        }

        /// <summary>Called by TrafficInteraction, or by your own collision code.</summary>
        public void Crash()
        {
            if (!Running) return;
            Running = false;
            Crashed = true;
            Combo = 0;
            player.Stop();
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
            OnCrashed?.Invoke();   // ContinuePanel shows, or calls Finish() if !CanContinue
        }

        /// <summary>After a rewarded ad (or free with Premium).</summary>
        public void Continue()
        {
            if (!Crashed || Finished || !CanContinue) return;
            continuesUsed++;
            if (SaveSystem.Data.premium) freeContinueUsed = true;
            road.ClearAround(player.transform.position.z, clearRadiusOnContinue);
            Crashed = false;
            player.Resume();
            Running = true;
            OnContinued?.Invoke();
        }

        public void SetPaused(bool paused)
        {
            if (Finished || Paused == paused) return;
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            OnPauseChanged?.Invoke(paused);
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && Running) SetPaused(true);
        }

        public void Finish()
        {
            if (Finished) return;
            Finished = true;
            Running = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            player.Stop();
            road.StopSpawning();

            if (rivals != null && rivals.Active)
            {
                Stats.rivalsBeaten = rivals.RivalsBeaten;
                scoreF += Stats.rivalsBeaten * pointsPerRivalBeaten;
                Stats.score = Mathf.FloorToInt(scoreF);
            }

            var d = SaveSystem.Data;
            var result = new RaceResult
            {
                stats = Stats,
                previousHighScore = d.highScore,
                newRecord = Stats.score > d.highScore,
                coins = Stats.score / Mathf.Max(1, scorePerCoin) * (d.premium ? 2 : 1),
                xp = Stats.score / Mathf.Max(1, scorePerXp),
            };

            if (result.newRecord) d.highScore = Stats.score;
            d.bestDistanceM = Mathf.Max(d.bestDistanceM, Stats.distanceM);
            Economy.Add(Currency.Credits, result.coins);
            Economy.AddXp(result.xp);
            MissionManager.ApplyRun(Stats);
            SaveSystem.Save();

            if (GameManager.I != null) GameManager.I.LastResult = result;
            OnFinished?.Invoke(result);
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}

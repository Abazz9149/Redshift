using UnityEngine;

namespace Redshift
{
    /// <summary>
    /// Manages your AICarController rivals for a race:
    ///  - turns them on/off per game mode (off in Two-Way: their lane logic assumes one-way traffic)
    ///  - matches their speed range to the player's selected car (so upgrades don't make them useless)
    ///  - race position for the HUD ("POS 2/3") and RivalsBeaten for the "DEFEAT AI CARS" mission
    /// </summary>
    public class RivalTracker : MonoBehaviour
    {
        public AICarController[] rivals;

        [Header("Modes with rivals")]
        public bool inEndless = true;
        public bool inTimeTrial = true;
        public bool inTwoWay = false;

        [Header("Balance")]
        public bool matchPlayerSpeed = true;

        public bool Active { get; private set; }
        public int Total => Active ? rivals.Length + 1 : 0;

        /// <summary>1 = first. Rival with raceGap > 0 is ahead of the player.</summary>
        public int Position
        {
            get
            {
                int pos = 1;
                if (Active) foreach (var r in rivals) if (r != null && r.raceGap > 0f) pos++;
                return pos;
            }
        }

        public int RivalsBeaten
        {
            get
            {
                int n = 0;
                if (Active) foreach (var r in rivals) if (r != null && r.raceGap < 0f) n++;
                return n;
            }
        }

        /// <summary>Called from RaceSession.Awake, before the AI cars run Start().</summary>
        public void Setup(GameMode mode, PlayerController player)
        {
            Active = rivals != null && rivals.Length > 0 &&
                     (mode == GameMode.Endless && inEndless ||
                      mode == GameMode.TimeTrial && inTimeTrial ||
                      mode == GameMode.TwoWay && inTwoWay);

            if (rivals == null) return;
            foreach (var r in rivals)
            {
                if (r == null) continue;
                if (!Active)
                {
                    r.disabledByRace = true;
                    r.gameObject.SetActive(false);
                    continue;
                }
                if (matchPlayerSpeed && player != null)
                {
                    r.maxSpeed = player.maxSpeed;
                    r.minSpeed = player.minSpeed;
                    r.acceleration = player.acceleration;
                }
            }
        }
    }
}

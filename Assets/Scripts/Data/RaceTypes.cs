using System;
using System.Collections.Generic;
using UnityEngine;

namespace Redshift
{
    public enum MissionType
    {
        Overtakes,        // "OVERTAKE 15 CARS"
        NearMisses,       // "5 NEAR-MISSES"
        DistanceMeters,   // "TRAVEL 4 KM" -> target 4000
        NitroUses,        // "USE NITRO 3 TIMES"
        ReachSpeedKmh,    // "HIT 220 KM/H"
        Score,            // "SCORE 10,000"
        BestCombo,        // "REACH A x5 COMBO"
        CompleteRuns,     // "PLAY 3 RACES"
        BeatRivals,       // "DEFEAT AI CARS 2" -> finish ahead of 2 AI rivals
    }

    /// <summary>Numbers collected during one race. Fed to missions and the results screen.</summary>
    [Serializable]
    public class RunStats
    {
        public GameMode mode;
        public int score;
        public float distanceM;
        public float topSpeedKmh;
        public float timeSeconds;
        public int overtakes;
        public int nearMisses;
        public int bestCombo;
        public int nitroUses;
        public int rivalsBeaten;

        public int Value(MissionType t)
        {
            switch (t)
            {
                case MissionType.Overtakes: return overtakes;
                case MissionType.NearMisses: return nearMisses;
                case MissionType.DistanceMeters: return Mathf.FloorToInt(distanceM);
                case MissionType.NitroUses: return nitroUses;
                case MissionType.ReachSpeedKmh: return Mathf.FloorToInt(topSpeedKmh);
                case MissionType.Score: return score;
                case MissionType.BestCombo: return bestCombo;
                case MissionType.CompleteRuns: return 1;
                case MissionType.BeatRivals: return rivalsBeaten;
            }
            return 0;
        }
    }

    [Serializable]
    public class RaceResult
    {
        public RunStats stats;
        public int previousHighScore;
        public bool newRecord;
        public int coins;
        public int xp;
        public bool coinsDoubled;
    }
}

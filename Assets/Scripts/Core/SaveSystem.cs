using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Redshift
{
    /// <summary>Everything that persists between sessions. Plain JSON in persistentDataPath.</summary>
    [Serializable]
    public class PlayerData
    {
        public int version = 1;

        // Wallet (top bar: CREDITS / DIAMONDS)
        public int credits = 500;
        public int diamonds = 20;

        // Player card (top-left: PLAYER  LV.42)
        public string playerName = "PLAYER";
        public int xp;

        // Records (results screen: HIGH SCORE / ALL-TIME BEST)
        public int highScore;
        public float bestDistanceM;

        // Garage
        public string selectedCarId = "starter";
        public List<string> ownedCars = new List<string> { "starter" };
        public List<CarUpgradeState> upgrades = new List<CarUpgradeState>();

        // Purchases
        public bool removeAds;
        public bool premium;

        // Daily rewards (7-day streak)
        public int rewardStreakIndex;          // 0..6 = next day to claim
        public long lastRewardClaimDay = -1;   // TimeUtil.Today of last claim

        // Daily missions
        public long missionsDay = -1;
        public List<MissionProgress> missions = new List<MissionProgress>();

        // Settings
        public float musicVolume = 0.75f;
        public float soundVolume = 0.85f;
        public int graphicsLevel = 2;          // 0 Low, 1 Medium, 2 High, 3 Ultra
        public int controlMode;                // 0 buttons, 1 steering wheel, 2 gyro
        public float steerSensitivity = 1f;

        // Stats used for interstitial pacing
        public int runsSinceInterstitial;
    }

    [Serializable]
    public class CarUpgradeState
    {
        public string carId;
        public int engine;
        public int handling;
        public int nitro;
    }

    [Serializable]
    public class MissionProgress
    {
        public string missionId;
        public int progress;
        public bool claimed;
    }

    public static class SaveSystem
    {
        static PlayerData data;
        static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        public static PlayerData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    data = JsonUtility.FromJson<PlayerData>(File.ReadAllText(FilePath));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Could not read save, starting fresh: " + e.Message);
                data = null;
            }
            if (data == null) data = new PlayerData();
        }

        public static void Save()
        {
            if (data == null) return;
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] Failed: " + e.Message);
            }
        }

        /// <summary>Editor helper: wipe progress.</summary>
        public static void ResetAll()
        {
            data = new PlayerData();
            Save();
        }
    }

    public static class TimeUtil
    {
        static readonly DateTime Epoch = new DateTime(2020, 1, 1);

        /// <summary>Local calendar day number. Daily rewards and missions reset at local midnight.</summary>
        public static long Today => (long)(DateTime.Now.Date - Epoch).TotalDays;

        public static TimeSpan UntilMidnight => DateTime.Today.AddDays(1) - DateTime.Now;

        public static string FormatReset(TimeSpan t) => $"{(int)t.TotalHours}H {t.Minutes:00}M";
    }
}

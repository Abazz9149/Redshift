using System;
using System.Collections.Generic;
using UnityEngine;

namespace Redshift
{
    /// <summary>DAILY MISSIONS (Frame 13): 3 missions picked per day from the pool, reset at local midnight.</summary>
    public static class MissionManager
    {
        static MissionDatabase db;
        public static event Action OnChanged;

        public class Entry
        {
            public MissionDefinition def;
            public MissionProgress progress;
            public bool Complete => progress.progress >= def.target;
            public bool Claimable => Complete && !progress.claimed;
        }

        public static void Init(MissionDatabase database)
        {
            db = database;
            EnsureToday();
        }

        /// <summary>Rolls a new set of missions if the day changed. Safe to call often.</summary>
        public static void EnsureToday()
        {
            if (db == null) return;
            var d = SaveSystem.Data;
            long today = TimeUtil.Today;
            if (d.missionsDay == today && d.missions.Count > 0) return;

            d.missionsDay = today;
            d.missions.Clear();

            var rng = new System.Random((int)today);
            var pool = new List<MissionDefinition>();
            foreach (var m in db.pool) if (m != null) pool.Add(m);

            var usedTypes = new HashSet<MissionType>();
            int guard = 100;
            while (d.missions.Count < db.dailyCount && pool.Count > 0 && guard-- > 0)
            {
                int k = rng.Next(pool.Count);
                var pick = pool[k];
                pool.RemoveAt(k);
                // Prefer variety: skip a second mission of the same type while other types remain.
                if (usedTypes.Contains(pick.type) && pool.Exists(p => !usedTypes.Contains(p.type))) continue;
                usedTypes.Add(pick.type);
                d.missions.Add(new MissionProgress { missionId = pick.id });
            }
            SaveSystem.Save();
            OnChanged?.Invoke();
        }

        public static List<Entry> Active()
        {
            EnsureToday();
            var list = new List<Entry>();
            if (db == null) return list;
            foreach (var p in SaveSystem.Data.missions)
            {
                var def = db.Get(p.missionId);
                if (def != null) list.Add(new Entry { def = def, progress = p });
            }
            return list;
        }

        /// <summary>Called once at the end of every run.</summary>
        public static void ApplyRun(RunStats stats)
        {
            EnsureToday();
            foreach (var e in Active())
            {
                if (e.progress.claimed) continue;
                int v = stats.Value(e.def.type);
                int next = e.def.singleRun ? Mathf.Max(e.progress.progress, v) : e.progress.progress + v;
                e.progress.progress = Mathf.Min(next, e.def.target);
            }
            SaveSystem.Save();
            OnChanged?.Invoke();
        }

        public static bool Claim(string missionId)
        {
            foreach (var e in Active())
            {
                if (e.def.id != missionId || !e.Claimable) continue;
                e.progress.claimed = true;
                int amount = e.def.rewardAmount * (SaveSystem.Data.premium ? 2 : 1);
                Economy.Add(e.def.rewardCurrency, amount);
                SaveSystem.Save();
                OnChanged?.Invoke();
                return true;
            }
            return false;
        }

        public static int ClaimableCount()
        {
            int n = 0;
            foreach (var e in Active()) if (e.Claimable) n++;
            return n;
        }
    }
}

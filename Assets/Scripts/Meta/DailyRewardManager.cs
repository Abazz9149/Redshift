using System;

namespace Redshift
{
    public enum RewardSlotState { Claimed, Ready, Upcoming }

    /// <summary>
    /// DAILY REWARDS (Frame 6): 7-day streak. Missing a full day resets to DAY 01.
    /// After DAY 07 is claimed the cycle starts again.
    /// </summary>
    public static class DailyRewardManager
    {
        static DailyRewardTable table;
        public static event Action OnChanged;

        public static int DayCount => table != null ? table.days.Count : 0;

        public static void Init(DailyRewardTable t)
        {
            table = t;
            CheckStreak();
        }

        static void CheckStreak()
        {
            var d = SaveSystem.Data;
            if (d.lastRewardClaimDay < 0) return;
            long gap = TimeUtil.Today - d.lastRewardClaimDay;
            if (gap > 1)
            {
                d.rewardStreakIndex = 0;   // streak broken
                d.lastRewardClaimDay = -1;
                SaveSystem.Save();
            }
        }

        public static bool CanClaimToday
        {
            get
            {
                CheckStreak();
                return table != null && SaveSystem.Data.lastRewardClaimDay != TimeUtil.Today;
            }
        }

        /// <summary>Index of the card that is READY (or will be tomorrow if already claimed today).</summary>
        public static int CurrentIndex => SaveSystem.Data.rewardStreakIndex;

        public static DailyReward Get(int day) => table.days[day];

        public static RewardSlotState StateOf(int day)
        {
            int idx = CurrentIndex;
            if (day < idx) return RewardSlotState.Claimed;
            if (day == idx && CanClaimToday) return RewardSlotState.Ready;
            return RewardSlotState.Upcoming;
        }

        public static bool Claim()
        {
            if (!CanClaimToday) return false;
            var d = SaveSystem.Data;
            var r = table.days[d.rewardStreakIndex];

            Economy.Add(r.currency, r.amount);
            if (r.car != null && !d.ownedCars.Contains(r.car.id)) d.ownedCars.Add(r.car.id);

            d.lastRewardClaimDay = TimeUtil.Today;
            d.rewardStreakIndex = (d.rewardStreakIndex + 1) % table.days.Count;
            SaveSystem.Save();
            OnChanged?.Invoke();
            return true;
        }
    }
}

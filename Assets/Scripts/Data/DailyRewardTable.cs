using System;
using System.Collections.Generic;
using UnityEngine;

namespace Redshift
{
    [Serializable]
    public class DailyReward
    {
        public Currency currency = Currency.Credits;
        public int amount = 100;
        [Tooltip("Optional: unlocks this car instead/in addition (good for DAY 07 GRAND PRIZE)")]
        public CarData car;
    }

    /// <summary>The 7 cards on the DAILY REWARDS screen.</summary>
    [CreateAssetMenu(menuName = "Redshift/Daily Reward Table", fileName = "DailyRewards")]
    public class DailyRewardTable : ScriptableObject
    {
        public List<DailyReward> days = new List<DailyReward>
        {
            new DailyReward { amount = 100 },
            new DailyReward { amount = 200 },
            new DailyReward { amount = 400 },
            new DailyReward { currency = Currency.Diamonds, amount = 10 },
            new DailyReward { amount = 800 },
            new DailyReward { amount = 1000 },
            new DailyReward { currency = Currency.Diamonds, amount = 50 },
        };
    }
}

using System;

namespace Redshift
{
    public enum Currency { Credits, Diamonds }

    /// <summary>Two currencies only: Credits (soft, earned by driving) and Diamonds (hard, rare / IAP).</summary>
    public static class Economy
    {
        public static event Action OnChanged;

        public static int Credits => SaveSystem.Data.credits;
        public static int Diamonds => SaveSystem.Data.diamonds;

        public static int Get(Currency c) => c == Currency.Credits ? Credits : Diamonds;

        public static void Add(Currency c, int amount)
        {
            if (amount <= 0) return;
            if (c == Currency.Credits) SaveSystem.Data.credits += amount;
            else SaveSystem.Data.diamonds += amount;
            SaveSystem.Save();
            OnChanged?.Invoke();
        }

        public static bool CanAfford(Currency c, int amount) => Get(c) >= amount;

        public static bool Spend(Currency c, int amount)
        {
            if (amount < 0 || !CanAfford(c, amount)) return false;
            if (c == Currency.Credits) SaveSystem.Data.credits -= amount;
            else SaveSystem.Data.diamonds -= amount;
            SaveSystem.Save();
            OnChanged?.Invoke();
            return true;
        }

        public static void AddXp(int amount)
        {
            SaveSystem.Data.xp += Math.Max(0, amount);
            OnChanged?.Invoke();
        }

        /// <summary>Call after something changes wallet-adjacent data (e.g. premium bought) so bars refresh.</summary>
        public static void NotifyChanged() => OnChanged?.Invoke();
    }
}

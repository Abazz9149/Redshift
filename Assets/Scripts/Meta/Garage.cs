using System;

namespace Redshift
{
    /// <summary>GARAGE (Frame 10): own, buy, select and upgrade cars.</summary>
    public static class Garage
    {
        public static event Action OnChanged;

        public static bool Owns(CarData car) => car != null && SaveSystem.Data.ownedCars.Contains(car.id);

        public static bool IsSelected(CarData car) => car != null && SaveSystem.Data.selectedCarId == car.id;

        public static bool IsLevelLocked(CarData car) =>
            GameManager.I != null && GameManager.I.PlayerLevel < car.requiredLevel;

        public static CarUpgradeState Upgrades(CarData car)
        {
            var list = SaveSystem.Data.upgrades;
            foreach (var u in list) if (u.carId == car.id) return u;
            var s = new CarUpgradeState { carId = car.id };
            list.Add(s);
            return s;
        }

        public static CarStats Stats(CarData car)
        {
            var u = Upgrades(car);
            return car.StatsAt(u.engine, u.handling, u.nitro);
        }

        public static int Level(CarData car, UpgradeType t)
        {
            var u = Upgrades(car);
            switch (t)
            {
                case UpgradeType.Engine: return u.engine;
                case UpgradeType.Handling: return u.handling;
                default: return u.nitro;
            }
        }

        public static bool IsMaxed(CarData car, UpgradeType t) => Level(car, t) >= car.maxUpgradeLevel;

        /// <summary>Cost grows each level: base, 2x, 3x...</summary>
        public static int UpgradeCost(CarData car, UpgradeType t) => car.upgradeBaseCost * (Level(car, t) + 1);

        public static bool Buy(CarData car)
        {
            if (Owns(car)) return true;
            if (IsLevelLocked(car)) return false;
            if (!Economy.Spend(car.priceCurrency, car.price)) return false;
            SaveSystem.Data.ownedCars.Add(car.id);
            SaveSystem.Save();
            OnChanged?.Invoke();
            return true;
        }

        public static bool Select(CarData car)
        {
            if (!Owns(car)) return false;
            SaveSystem.Data.selectedCarId = car.id;
            SaveSystem.Save();
            OnChanged?.Invoke();
            return true;
        }

        public static bool Upgrade(CarData car, UpgradeType t)
        {
            if (!Owns(car) || IsMaxed(car, t)) return false;
            if (!Economy.Spend(Currency.Credits, UpgradeCost(car, t))) return false;
            var u = Upgrades(car);
            switch (t)
            {
                case UpgradeType.Engine: u.engine++; break;
                case UpgradeType.Handling: u.handling++; break;
                case UpgradeType.Nitro: u.nitro++; break;
            }
            SaveSystem.Save();
            OnChanged?.Invoke();
            return true;
        }
    }
}

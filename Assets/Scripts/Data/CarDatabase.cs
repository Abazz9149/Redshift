using System.Collections.Generic;
using UnityEngine;

namespace Redshift
{
    [CreateAssetMenu(menuName = "Redshift/Car Database", fileName = "CarDatabase")]
    public class CarDatabase : ScriptableObject
    {
        public List<CarData> cars = new List<CarData>();

        public CarData Get(string id)
        {
            foreach (var c in cars) if (c != null && c.id == id) return c;
            return null;
        }

        public int IndexOf(string id)
        {
            for (int i = 0; i < cars.Count; i++) if (cars[i] != null && cars[i].id == id) return i;
            return 0;
        }

        /// <summary>Max values across all cars, for normalising garage stat bars.</summary>
        public CarStats MaxStats()
        {
            var m = new CarStats();
            foreach (var c in cars)
            {
                if (c == null) continue;
                var s = c.StatsAt(c.maxUpgradeLevel, c.maxUpgradeLevel, c.maxUpgradeLevel);
                m.topSpeedKmh = Mathf.Max(m.topSpeedKmh, s.topSpeedKmh);
                m.acceleration = Mathf.Max(m.acceleration, s.acceleration);
                m.handling = Mathf.Max(m.handling, s.handling);
                m.nitroSeconds = Mathf.Max(m.nitroSeconds, s.nitroSeconds);
            }
            return m;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Redshift
{
    [CreateAssetMenu(menuName = "Redshift/Mission Database", fileName = "MissionDatabase")]
    public class MissionDatabase : ScriptableObject
    {
        public int dailyCount = 3;
        public List<MissionDefinition> pool = new List<MissionDefinition>();

        public MissionDefinition Get(string id)
        {
            foreach (var m in pool) if (m != null && m.id == id) return m;
            return null;
        }
    }
}

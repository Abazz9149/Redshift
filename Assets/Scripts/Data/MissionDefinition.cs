using System;
using System.Collections.Generic;
using UnityEngine;

namespace Redshift
{
    /// <summary>Create via Assets > Create > Redshift > Mission.</summary>
    [CreateAssetMenu(menuName = "Redshift/Mission", fileName = "Mission_")]
    public class MissionDefinition : ScriptableObject
    {
        public string id = "overtake_15";
        public string title = "OVERTAKE 15 CARS";
        [TextArea] public string description = "Pass 15 other cars in any race";
        public string tag = "SPRINT OBJECTIVE";
        public MissionType type = MissionType.Overtakes;
        public int target = 15;
        [Tooltip("ON = must be done in one run (e.g. score 10,000). OFF = adds up across runs.")]
        public bool singleRun;
        public Currency rewardCurrency = Currency.Credits;
        public int rewardAmount = 500;

        public string FormatProgress(int value)
        {
            if (type == MissionType.DistanceMeters)
                return $"{value / 1000f:0.#} / {target / 1000f:0.#} KM";
            return $"{value:N0} / {target:N0}";
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Redshift
{
    public enum UpgradeType { Engine, Handling, Nitro }

    public struct CarStats
    {
        public float topSpeedKmh;    // -> PlayerController.maxSpeed (the number your HUD shows)
        public float acceleration;   // -> PlayerController.acceleration
        public float handling;       // -> PlayerController.steerSpeed
        public float nitroSeconds;   // -> PlayerController.nitroSeconds
    }

    /// <summary>One car in the garage (Frame 10). Create via Assets > Create > Redshift > Car.</summary>
    [CreateAssetMenu(menuName = "Redshift/Car", fileName = "Car_")]
    public class CarData : ScriptableObject
    {
        [Header("Identity")]
        public string id = "starter";
        public string displayName = "THE POWER GT";
        public string carClass = "C";            // shown as CLASS C / B / A / S+
        public GameObject prefab;                // visual only, no physics needed
        public Sprite thumbnail;

        [Header("Price (0 = free)")]
        public Currency priceCurrency = Currency.Credits;
        public int price;
        public int requiredLevel = 1;

        [Header("Base stats (same units as PlayerController)")]
        public float baseTopSpeedKmh = 120f;
        public float baseAcceleration = 15f;
        public float baseHandling = 12f;
        public float baseNitroSeconds = 3f;

        [Header("Upgrades (per level)")]
        public int maxUpgradeLevel = 5;
        public int upgradeBaseCost = 400;
        public float topSpeedPerLevel = 8f;
        public float accelerationPerLevel = 1.5f;
        public float handlingPerLevel = 0.8f;
        public float nitroPerLevel = 0.5f;

        public CarStats StatsAt(int engine, int handling, int nitro) => new CarStats
        {
            topSpeedKmh = baseTopSpeedKmh + engine * topSpeedPerLevel,
            acceleration = baseAcceleration + engine * accelerationPerLevel,
            handling = baseHandling + handling * handlingPerLevel,
            nitroSeconds = baseNitroSeconds + nitro * nitroPerLevel,
        };
    }
}

using System;
using UnityEngine;

namespace WarForge.Data
{
    public enum ShellType { AP, HE }

    [Serializable]
    public class ShellSpec
    {
        public string name;
        public ShellType type;
        public float caliberMm;
        public float muzzleVelocity;          // m/s
        public float penetration;             // mm of RHA at 0 m, 0 degrees
        public float penetrationLossPer1000m; // fraction lost per 1000 m (0.2 = 20%)
        public float damage;                  // base internal damage on penetration
        public float explosiveRadius;         // m, HE only
        public float normalizationDeg;        // AP shells "bite" into sloped armor a little

        public float PenetrationAt(float meters)
        {
            float loss = Mathf.Clamp01(penetrationLossPer1000m * Mathf.Max(0f, meters) / 1000f);
            return Mathf.Max(0f, penetration * (1f - loss));
        }

        public string ShortName => type == ShellType.AP ? "AP" : "HE";
    }
}

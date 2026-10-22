using System.Collections.Generic;
using UnityEngine;

namespace WarForge.Data
{
    /// <summary>All vehicles in the game. Original fictional designs, not real-world or third-party assets.</summary>
    public static class VehicleCatalog
    {
        static List<VehicleSpec> all;

        public static IReadOnlyList<VehicleSpec> All
        {
            get
            {
                if (all == null) Build();
                return all;
            }
        }

        public static VehicleSpec Get(string id)
        {
            foreach (var v in All) if (v.id == id) return v;
            return All[0];
        }

        public static bool Exists(string id)
        {
            foreach (var v in All) if (v.id == id) return true;
            return false;
        }

        public static List<VehicleSpec> PlayerVehicles()
        {
            var list = new List<VehicleSpec>();
            foreach (var v in All) if (v.playerSelectable) list.Add(v);
            return list;
        }

        static ShellSpec AP(string name, float cal, float v, float pen, float loss, float dmg) => new ShellSpec
        {
            name = name, type = ShellType.AP, caliberMm = cal, muzzleVelocity = v, penetration = pen,
            penetrationLossPer1000m = loss, damage = dmg, explosiveRadius = 0f, normalizationDeg = 4f
        };

        static ShellSpec HE(string name, float cal, float v, float pen, float dmg, float radius) => new ShellSpec
        {
            name = name, type = ShellType.HE, caliberMm = cal, muzzleVelocity = v, penetration = pen,
            penetrationLossPer1000m = 0f, damage = dmg, explosiveRadius = radius, normalizationDeg = 0f
        };

        static VehicleSpec Light(string id, string name, bool player, Color a, Color b, Color c, int seed) => new VehicleSpec
        {
            id = id, displayName = name, vehicleClass = VehicleClass.Light, playerSelectable = player,
            description = "Fast scout tank. Thin armor, quick reload, excellent mobility. Flank and strike sides.",
            massKg = 16000f, hullSize = new Vector3(2.6f, 1.0f, 5.0f), turretSize = new Vector3(1.6f, 0.7f, 1.9f),
            turretOffsetZ = 0.35f, barrelLength = 2.8f, barrelRadius = 0.05f, roadWheelsPerSide = 5,
            maxForwardKph = 62f, maxReverseKph = 18f, enginePowerHp = 320f, pivotTurnRateDeg = 55f,
            turretTraverseDeg = 40f, gunElevationSpeedDeg = 10f, minElevationDeg = -10f, maxElevationDeg = 25f,
            reloadSeconds = 3.2f, structureHp = 650f,
            armor = new ArmorSpec
            {
                upperFront = 25f, upperFrontSlopeDeg = 50f, hullFront = 30f, hullSide = 20f, hullRear = 15f, hullTop = 10f, hullBottom = 8f,
                turretFront = 40f, turretSide = 25f, turretRear = 20f, turretTop = 10f, mantlet = 45f, track = 15f
            },
            shells = new[] { AP("L-50 AP", 50f, 820f, 85f, 0.25f, 200f), HE("L-50 HE", 50f, 600f, 10f, 150f, 4f) },
            camoA = a, camoB = b, camoC = c, camoSeed = seed
        };

        static VehicleSpec Medium(string id, string name, bool player, float pen, Color a, Color b, Color c, int seed) => new VehicleSpec
        {
            id = id, displayName = name, vehicleClass = VehicleClass.Medium, playerSelectable = player,
            description = "Balanced medium tank. Sloped glacis, reliable 75 mm gun, good all-rounder.",
            massKg = 30000f, hullSize = new Vector3(3.0f, 1.15f, 6.0f), turretSize = new Vector3(1.9f, 0.85f, 2.4f),
            turretOffsetZ = 0.3f, barrelLength = 3.6f, barrelRadius = 0.07f, roadWheelsPerSide = 6,
            maxForwardKph = 45f, maxReverseKph = 12f, enginePowerHp = 500f, pivotTurnRateDeg = 40f,
            turretTraverseDeg = 28f, gunElevationSpeedDeg = 8f, minElevationDeg = -8f, maxElevationDeg = 20f,
            reloadSeconds = 6.0f, structureHp = 1000f,
            armor = new ArmorSpec
            {
                upperFront = 50f, upperFrontSlopeDeg = 55f, hullFront = 60f, hullSide = 35f, hullRear = 25f, hullTop = 15f, hullBottom = 12f,
                turretFront = 80f, turretSide = 50f, turretRear = 40f, turretTop = 20f, mantlet = 90f, track = 20f
            },
            shells = new[] { AP("M-75 APCBC", 75f, 780f, pen, 0.18f, 320f), HE("M-75 HE", 75f, 560f, 14f, 260f, 6f) },
            camoA = a, camoB = b, camoC = c, camoSeed = seed
        };

        static VehicleSpec Heavy(string id, string name, bool player, string unlock, Color a, Color b, Color c, int seed) => new VehicleSpec
        {
            id = id, displayName = name, vehicleClass = VehicleClass.Heavy, playerSelectable = player, unlockedByMission = unlock,
            description = "Heavy breakthrough tank. Thick flat armor and a devastating 88 mm gun. Slow turret, slow hull.",
            massKg = 56000f, hullSize = new Vector3(3.6f, 1.35f, 6.8f), turretSize = new Vector3(2.3f, 1.0f, 2.8f),
            turretOffsetZ = 0.2f, barrelLength = 4.6f, barrelRadius = 0.09f, roadWheelsPerSide = 7,
            maxForwardKph = 36f, maxReverseKph = 10f, enginePowerHp = 700f, pivotTurnRateDeg = 28f,
            turretTraverseDeg = 18f, gunElevationSpeedDeg = 6f, minElevationDeg = -8f, maxElevationDeg = 16f,
            reloadSeconds = 8.5f, structureHp = 1500f,
            armor = new ArmorSpec
            {
                upperFront = 100f, upperFrontSlopeDeg = 15f, hullFront = 100f, hullSide = 80f, hullRear = 80f, hullTop = 25f, hullBottom = 25f,
                turretFront = 110f, turretSide = 80f, turretRear = 80f, turretTop = 25f, mantlet = 130f, track = 25f
            },
            shells = new[] { AP("H-88 APCBC", 88f, 810f, 170f, 0.12f, 420f), HE("H-88 HE", 88f, 650f, 18f, 380f, 8f) },
            camoA = a, camoB = b, camoC = c, camoSeed = seed
        };

        static void Build()
        {
            var olive = new Color(0.33f, 0.37f, 0.22f); var oliveDark = new Color(0.22f, 0.25f, 0.15f); var mud = new Color(0.42f, 0.36f, 0.25f);
            var sand = new Color(0.62f, 0.55f, 0.38f); var sandDark = new Color(0.45f, 0.40f, 0.28f); var green = new Color(0.30f, 0.35f, 0.24f);
            var grey = new Color(0.34f, 0.35f, 0.36f); var greyDark = new Color(0.22f, 0.23f, 0.24f); var rust = new Color(0.40f, 0.30f, 0.24f);

            all = new List<VehicleSpec>
            {
                Medium("wf_warden", "WF-4 Warden", true, 125f, olive, oliveDark, mud, 11),
                Light("wf_lynx", "WF-2 Lynx", true, sand, sandDark, green, 23),
                Heavy("wf_bastion", "WF-6 Bastion", true, "ironvale_assault", olive, mud, oliveDark, 37),

                Light("en_skirmisher", "Skirmisher", false, grey, greyDark, rust, 51),
                Medium("en_kestrel", "Kestrel", false, 115f, grey, greyDark, rust, 63),
                Heavy("en_goliath", "Goliath", false, "", greyDark, grey, rust, 77),
            };
            all[4].description = "Enemy medium tank. Comparable to the Warden.";
            all[3].description = "Enemy scout. Fast and fragile.";
            all[5].description = "Enemy heavy tank. Do not trade frontally: flank it.";
        }
    }
}

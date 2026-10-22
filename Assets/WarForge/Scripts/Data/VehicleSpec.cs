using System;
using UnityEngine;

namespace WarForge.Data
{
    public enum VehicleClass { Light, Medium, Heavy }

    [Serializable]
    public class ArmorSpec
    {
        public float upperFront, upperFrontSlopeDeg; // glacis plate thickness and slope from vertical
        public float hullFront, hullSide, hullRear, hullTop, hullBottom;
        public float turretFront, turretSide, turretRear, turretTop, mantlet;
        public float track = 20f;
    }

    /// <summary>
    /// Data describing one vehicle. Kept as a plain serializable class so it can later be moved
    /// to ScriptableObject assets or JSON files without changing gameplay code.
    /// </summary>
    [Serializable]
    public class VehicleSpec
    {
        public string id;
        public string displayName;
        public VehicleClass vehicleClass;
        public string description;
        public bool playerSelectable;
        public string unlockedByMission; // empty = always available

        public float massKg;
        public Vector3 hullSize;      // total width (incl. tracks), hull height, length
        public Vector3 turretSize;    // width, height, length
        public float turretOffsetZ;
        public float barrelLength, barrelRadius;
        public int roadWheelsPerSide = 5;

        public float maxForwardKph, maxReverseKph, enginePowerHp, pivotTurnRateDeg;
        public float turretTraverseDeg, gunElevationSpeedDeg, minElevationDeg, maxElevationDeg;
        public float reloadSeconds;
        public float structureHp;

        public ArmorSpec armor;
        public ShellSpec[] shells;

        public Color camoA, camoB, camoC;
        public int camoSeed;

        public float MaxForwardMs => maxForwardKph / 3.6f;
        public float MaxReverseMs => maxReverseKph / 3.6f;
    }
}

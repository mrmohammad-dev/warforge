using System;
using UnityEngine;
using WarForge.Vehicles;

namespace WarForge.Combat
{
    public struct HitReport
    {
        public Vehicle Attacker;
        public Vehicle Victim;
        public HitOutcome Outcome;
        public float Penetration;
        public float EffectiveArmor;
        public string PlateName;
        public string ModulesSummary;
        public bool IsHE;
        public Vector3 Point;
    }

    /// <summary>Global combat event hub used by HUD, missions and AI.</summary>
    public static class CombatEvents
    {
        public static event Action<HitReport> HitReported;
        public static event Action<Vehicle, Vehicle> VehicleDestroyed; // victim, killer

        public static void ReportHit(HitReport report) => HitReported?.Invoke(report);
        public static void ReportDestroyed(Vehicle victim, Vehicle killer) => VehicleDestroyed?.Invoke(victim, killer);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            HitReported = null;
            VehicleDestroyed = null;
        }
    }
}

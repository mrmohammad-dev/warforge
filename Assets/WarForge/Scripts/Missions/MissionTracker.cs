using System.Collections.Generic;
using UnityEngine;
using WarForge.Combat;
using WarForge.Vehicles;

namespace WarForge.Missions
{
    public enum MissionState { Running, Won, Lost }

    /// <summary>Counts kills, time and player survival for one mission run. Pure logic plus event hookup.</summary>
    public class MissionTracker
    {
        public MissionDefinition Definition;
        public MissionState State { get; private set; } = MissionState.Running;
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public int EnemiesTotal { get; private set; }
        public int EnemiesLeft => Mathf.Max(0, EnemiesTotal - Kills);
        public string LossReason { get; private set; }

        public float TimeRemaining => Definition.TimeLimitSeconds > 0f ? Mathf.Max(0f, Definition.TimeLimitSeconds - Elapsed) : -1f;

        readonly HashSet<Vehicle> enemies = new HashSet<Vehicle>();
        Vehicle player;

        public MissionTracker(MissionDefinition def) { Definition = def; }

        public void Begin(Vehicle playerVehicle, IEnumerable<Vehicle> enemyVehicles)
        {
            player = playerVehicle;
            enemies.Clear();
            foreach (var e in enemyVehicles) enemies.Add(e);
            EnemiesTotal = enemies.Count;
            Kills = 0;
            Elapsed = 0f;
            State = MissionState.Running;
            CombatEvents.VehicleDestroyed += OnDestroyed;
        }

        public void End()
        {
            CombatEvents.VehicleDestroyed -= OnDestroyed;
        }

        public void Tick(float dt)
        {
            if (State != MissionState.Running) return;
            Elapsed += dt;
            if (player == null || player.Damage == null || player.Damage.IsDestroyed)
            {
                Fail("Your tank was destroyed.");
            }
            else if (Definition.TimeLimitSeconds > 0f && Elapsed >= Definition.TimeLimitSeconds)
            {
                Fail("Time ran out.");
            }
        }

        void OnDestroyed(Vehicle victim, Vehicle killer)
        {
            if (State != MissionState.Running || victim == null) return;
            if (enemies.Remove(victim))
            {
                Kills++;
                if (enemies.Count == 0) State = MissionState.Won;
            }
        }

        void Fail(string reason)
        {
            State = MissionState.Lost;
            LossReason = reason;
        }
    }
}

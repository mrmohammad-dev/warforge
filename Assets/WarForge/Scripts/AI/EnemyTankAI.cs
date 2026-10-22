using UnityEngine;
using WarForge.Combat;
using WarForge.Core;
using WarForge.Vehicles;

namespace WarForge.AI
{
    public enum AIState { Patrol, Engage, Reposition, Dead }

    /// <summary>
    /// Simple but readable tank brain: patrol waypoints, spot the nearest player vehicle through line of sight,
    /// stop at preferred range, turn the turret with lead, and fire when on target and the line of fire is clear.
    /// Designed to be replaced by a utility/behaviour-tree system later without changing Vehicle or TankGun.
    /// </summary>
    [RequireComponent(typeof(Vehicle))]
    public class EnemyTankAI : MonoBehaviour
    {
        public Vehicle Target;
        public float SightRange = 260f;
        public float PreferredRange = 140f;
        public float Accuracy = 0.85f;     // 1 = perfect aim
        public Vector3[] Waypoints;
        public float AlertTime = 0f;
        public AIState State = AIState.Patrol;

        Vehicle self;
        int wp;
        float fireCooldown;
        float alertTimer;
        float rethinkTimer;

        void Awake() => self = GetComponent<Vehicle>();

        void Update()
        {
            if (self == null || !self.IsAlive || self.Controller == null) { State = AIState.Dead; return; }
            rethinkTimer -= Time.deltaTime;
            fireCooldown -= Time.deltaTime;

            if (rethinkTimer <= 0f)
            {
                rethinkTimer = 0.25f;
                Target = FindTarget();
            }
            if (Target != null) alertTimer = 4f;
            else alertTimer -= Time.deltaTime;

            State = Target != null ? AIState.Engage : (alertTimer > 0f ? AIState.Reposition : AIState.Patrol);
            switch (State)
            {
                case AIState.Engage: Engage(); break;
                case AIState.Reposition: Engage(); break;
                default: Patrol(); break;
            }
        }

        Vehicle FindTarget()
        {
            Vehicle best = null;
            float bestD = SightRange;
            foreach (var v in Vehicle.All)
            {
                if (v == null || v.Team == self.Team || !v.IsAlive) continue;
                float d = Vector3.Distance(transform.position, v.transform.position);
                if (d > bestD) continue;
                Vector3 from = self.SightPoint != null ? self.SightPoint.position : transform.position + Vector3.up;
                Vector3 to = v.CenterOfMassWorld;
                if (Physics.Linecast(from, to, out RaycastHit h, ~0, QueryTriggerInteraction.Ignore) && h.collider.attachedRigidbody != v.Body) continue;
                bestD = d; best = v;
            }
            return best;
        }

        void Patrol()
        {
            var c = self.Controller;
            c.Throttle = 0.5f;
            if (Waypoints == null || Waypoints.Length == 0) { c.Throttle = 0f; c.Steer = 0f; self.Turret.ClearAim(); return; }
            Vector3 goal = Waypoints[wp % Waypoints.Length];
            if (Vector3.Distance(transform.position, goal) < 12f) { wp = (wp + 1) % Waypoints.Length; }
            Drive(goal);
            self.Turret.SetAimPoint(transform.position + transform.forward * 60f);
        }

        void Engage()
        {
            if (Target == null) { Patrol(); return; }
            var c = self.Controller;
            float d = Vector3.Distance(transform.position, Target.transform.position);

            // Keep distance and flank: drive toward a point offset to the side of the target.
            Vector3 toTarget = (Target.transform.position - transform.position);
            Vector3 flank = Vector3.Cross(Vector3.up, toTarget.normalized) * 35f;
            Vector3 goal = d > PreferredRange * 1.2f ? Target.transform.position + flank : transform.position - toTarget.normalized * 30f + flank;
            Drive(goal);

            // Aim with lead using the shell that is currently loaded.
            var shell = self.Gun.Current;
            if (shell != null)
            {
                Vector3 aim = BallisticsMath.ComputeAimPoint(self.Muzzle.position, Target.CenterOfMassWorld, Target.Velocity, shell.muzzleVelocity, Physics.gravity.magnitude);
                // Inaccuracy grows with range and is reduced by accuracy setting.
                float spread = (1f - Accuracy) * d * 0.03f;
                aim += Random.insideUnitSphere * spread;
                self.Turret.SetAimPoint(aim);

                if (self.Turret.OnTarget && self.Gun.Ready && fireCooldown <= 0f && d < 400f)
                {
                    if (self.Gun.TryFire()) fireCooldown = Random.Range(0.6f, 1.6f);
                }
            }
        }

        void Drive(Vector3 goal)
        {
            var c = self.Controller;
            Vector3 local = transform.InverseTransformPoint(goal);
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            c.Steer = Mathf.Clamp(angle / 35f, -1f, 1f);
            float speedFactor = Mathf.Clamp01(1f - Mathf.Abs(angle) / 80f);
            c.Throttle = Mathf.Clamp01(speedFactor);
            if (local.z < -4f) c.Throttle = -0.5f;
        }
    }
}

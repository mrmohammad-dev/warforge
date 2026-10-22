using System.Collections.Generic;
using UnityEngine;
using WarForge.Core;
using WarForge.Data;
using WarForge.FX;
using WarForge.Vehicles;
using WarForge.World;

namespace WarForge.Combat
{
    /// <summary>
    /// Ballistic shell with manual integration and segment raycasts (no rigidbody, no tunnelling).
    /// Handles ricochets, spaced armor (tracks), penetration, HE blast and environment hits.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        const float MaxLifetime = 5f;
        static readonly RaycastHit[] Hits = new RaycastHit[16];

        ShellSpec shell;
        Vehicle owner;
        Rigidbody ownerBody;
        Vector3 velocity;
        Vector3 launchPoint;
        float life;
        float penetrationSpent;
        int ricochets;
        Collider lastCollider;
        TrailRenderer trail;
        bool active;

        public void Launch(ShellSpec spec, Vehicle shooter, Vector3 position, Vector3 initialVelocity)
        {
            shell = spec;
            owner = shooter;
            ownerBody = shooter != null ? shooter.Body : null;
            velocity = initialVelocity;
            launchPoint = position;
            transform.position = position;
            life = 0f;
            penetrationSpent = 0f;
            ricochets = 0;
            lastCollider = null;
            active = true;
            if (trail == null) trail = EffectsManager.CreateTracer(gameObject, spec.type == ShellType.HE);
            trail.Clear();
            trail.emitting = true;
        }

        void Update()
        {
            if (!active) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            life += dt;
            if (life > MaxLifetime) { Despawn(); return; }

            // Two sub-steps keep the arc smooth at low frame rates.
            float h = dt * 0.5f;
            for (int s = 0; s < 2 && active; s++)
            {
                Vector3 pos = transform.position;
                Vector3 next = pos + velocity * h + 0.5f * h * h * Physics.gravity;
                Vector3 seg = next - pos;
                float len = seg.magnitude;
                if (len > 1e-5f && CastSegment(pos, seg / len, len, out RaycastHit hit))
                {
                    HandleHit(hit, seg / len);
                }
                else
                {
                    velocity += Physics.gravity * h;
                    transform.position = next;
                }
            }
            if (active && transform.position.y < -200f) Despawn();
        }

        bool CastSegment(Vector3 origin, Vector3 dir, float dist, out RaycastHit best)
        {
            best = default;
            int n = Physics.RaycastNonAlloc(origin, dir, Hits, dist, ~0, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var c = Hits[i].collider;
                if (c == null || c == lastCollider) continue;
                if (ownerBody != null && c.attachedRigidbody == ownerBody) continue;
                if (Hits[i].distance < bestDist) { bestDist = Hits[i].distance; best = Hits[i]; found = true; }
            }
            return found;
        }

        void HandleHit(RaycastHit hit, Vector3 dir)
        {
            transform.position = hit.point;
            float traveled = Vector3.Distance(launchPoint, hit.point);
            float pen = Mathf.Max(0f, shell.PenetrationAt(traveled) - penetrationSpent);
            bool he = shell.type == ShellType.HE;

            var plate = hit.collider.GetComponent<ArmorPlate>();
            if (plate != null && plate.Vehicle != null && plate.Vehicle.Damage != null && !plate.Vehicle.Damage.IsDestroyed)
            {
                HandleArmorHit(plate, hit, dir, pen, he);
                return;
            }

            // Environment.
            var destructible = hit.collider.GetComponentInParent<Destructible>();
            if (destructible != null) destructible.ApplyDamage(he ? shell.damage * 2f : shell.damage * 0.8f, hit.point, dir * shell.caliberMm * 40f);
            var tree = hit.collider.GetComponentInParent<TreeProp>();
            if (tree != null) tree.Topple(dir * 4000f, hit.point);

            if (he) Explode(hit.point, null);
            else
            {
                EffectsManager.GroundImpact(hit.point, hit.normal, shell.caliberMm / 75f);
                AudioManager.PlayImpact(hit.point, false);
            }
            Despawn();
        }

        void HandleArmorHit(ArmorPlate plate, RaycastHit hit, Vector3 dir, float pen, bool he)
        {
            var victim = plate.Vehicle;
            float thickness = plate.ThicknessFor(hit.normal, out string face);
            float angle = ArmorMath.ImpactAngleDeg(dir, hit.normal);
            float ricochetAngle = he ? 89f : ArmorMath.DefaultRicochetAngle;
            var outcome = ArmorMath.Evaluate(pen, thickness, angle, ricochetAngle, shell.normalizationDeg, out float effective);

            var report = new HitReport
            {
                Attacker = owner, Victim = victim, Outcome = outcome, Penetration = pen, EffectiveArmor = effective,
                PlateName = face, IsHE = he, Point = hit.point, ModulesSummary = ""
            };

            if (plate.IsTrack)
            {
                // Tracks act as spaced armor: they break, and the shell continues with reduced penetration.
                if (outcome == HitOutcome.Penetration)
                {
                    report.ModulesSummary = victim.Damage.DamageModuleDirect(plate.TrackModule, shell.damage * (he ? 1.2f : 0.8f), owner);
                    CombatEvents.ReportHit(report);
                    EffectsManager.Sparks(hit.point, hit.normal);
                    if (he) { Explode(hit.point, victim); Despawn(); return; }
                    penetrationSpent += effective;
                    lastCollider = hit.collider;
                    transform.position = hit.point + dir * 0.02f;
                    velocity *= 0.95f;
                    return;
                }
            }

            switch (outcome)
            {
                case HitOutcome.Ricochet:
                    CombatEvents.ReportHit(report);
                    EffectsManager.Sparks(hit.point, hit.normal);
                    AudioManager.PlayRicochet(hit.point);
                    ricochets++;
                    if (ricochets > 1) { Despawn(); return; }
                    velocity = Vector3.Reflect(velocity, hit.normal) * 0.6f;
                    penetrationSpent += pen * 0.5f;
                    lastCollider = hit.collider;
                    transform.position = hit.point + hit.normal * 0.05f;
                    return;

                case HitOutcome.NonPenetration:
                    if (he)
                    {
                        Explode(hit.point, null);
                        report.ModulesSummary = "";
                    }
                    else
                    {
                        EffectsManager.Sparks(hit.point, hit.normal);
                        AudioManager.PlayImpact(hit.point, true);
                    }
                    CombatEvents.ReportHit(report);
                    Despawn();
                    return;

                default:
                    report.ModulesSummary = victim.Damage.ApplyPenetration(hit.point, dir, shell.damage * (he ? 1.3f : 1f), owner);
                    CombatEvents.ReportHit(report);
                    EffectsManager.Penetration(hit.point, dir);
                    AudioManager.PlayImpact(hit.point, true);
                    if (he) Explode(hit.point, victim);
                    Despawn();
                    return;
            }
        }

        void Explode(Vector3 point, Vehicle exclude)
        {
            Explosions.Blast(point, shell.explosiveRadius, shell.damage, shell.penetration, owner, exclude);
            EffectsManager.Explosion(point, Mathf.Max(0.5f, shell.explosiveRadius / 6f));
            AudioManager.PlayExplosion(point, 0.6f);
        }

        void Despawn()
        {
            active = false;
            if (trail != null) trail.emitting = false;
            ProjectilePool.Release(this);
        }
    }

    public static class Explosions
    {
        static readonly Collider[] Overlap = new Collider[64];
        static readonly HashSet<Vehicle> Vehicles = new HashSet<Vehicle>();
        static readonly HashSet<Destructible> Props = new HashSet<Destructible>();

        public static void Blast(Vector3 point, float radius, float damage, float hePen, Vehicle attacker, Vehicle exclude)
        {
            if (radius <= 0f) return;
            Vehicles.Clear();
            Props.Clear();
            int n = Physics.OverlapSphereNonAlloc(point, radius, Overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = Overlap[i];
                if (c == null) continue;
                var plate = c.GetComponent<ArmorPlate>();
                if (plate != null && plate.Vehicle != null && plate.Vehicle != exclude) Vehicles.Add(plate.Vehicle);
                var d = c.GetComponentInParent<Destructible>();
                if (d != null) Props.Add(d);
            }
            foreach (var v in Vehicles)
            {
                if (v == null || v.Damage == null || v.Damage.IsDestroyed) continue;
                string s = v.Damage.ApplyBlast(point, radius, damage, hePen, attacker);
                if (!string.IsNullOrEmpty(s))
                    CombatEvents.ReportHit(new HitReport { Attacker = attacker, Victim = v, Outcome = HitOutcome.Penetration, IsHE = true, PlateName = "Blast", ModulesSummary = s, Point = point, Penetration = hePen });
            }
            foreach (var d in Props)
            {
                if (d == null) continue;
                float dist = Vector3.Distance(point, d.transform.position);
                d.ApplyDamage(damage * Mathf.Clamp01(1.2f - dist / (radius * 2f)), point, (d.transform.position - point).normalized * damage * 10f);
            }
        }
    }
}

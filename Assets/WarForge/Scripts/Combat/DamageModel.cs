using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WarForge.FX;
using WarForge.Vehicles;

namespace WarForge.Combat
{
    public enum ModuleType { Driver, Gunner, Commander, Loader, Engine, AmmoRack, Breech, TurretDrive, TrackLeft, TrackRight }

    public class DamageModule
    {
        public ModuleType Type;
        public Transform Space;  // hull root or turret, so turret modules rotate with the turret
        public Bounds LocalBounds;
        public float MaxHp;
        public float Hp;
        public float RepairCountdown;

        public bool Destroyed => Hp <= 0f;
        public float Health01 => MaxHp > 0f ? Hp / MaxHp : 0f;
        public bool IsCrew => Type == ModuleType.Driver || Type == ModuleType.Gunner || Type == ModuleType.Commander || Type == ModuleType.Loader;
        public bool IsExternal => Type == ModuleType.TrackLeft || Type == ModuleType.TrackRight;
        public bool Repairable => !IsCrew && Type != ModuleType.AmmoRack;

        public string DisplayName
        {
            get
            {
                switch (Type)
                {
                    case ModuleType.AmmoRack: return "Ammo rack";
                    case ModuleType.TurretDrive: return "Turret drive";
                    case ModuleType.TrackLeft: return "Left track";
                    case ModuleType.TrackRight: return "Right track";
                    default: return Type.ToString();
                }
            }
        }
    }

    /// <summary>
    /// Component-based damage: shells that penetrate trace a main path plus spall fragments through internal modules.
    /// Crew loss, ammo detonation, structure loss or fire can destroy the vehicle.
    /// </summary>
    public class DamageModel : MonoBehaviour
    {
        public const float RepairSeconds = 15f;
        public readonly List<DamageModule> Modules = new List<DamageModule>();
        public float Structure;
        public float MaxStructure;
        public bool IsDestroyed { get; private set; }
        public bool OnFire { get; private set; }
        public bool AmmoDetonated { get; private set; }
        public Vehicle LastAttacker { get; private set; }
        public int Version { get; private set; } // increments on every change (HUD uses it to refresh cheaply)

        Vehicle vehicle;
        float fireTime;
        bool pendingDetonation;
        ParticleSystem fireFx;
        readonly List<DamageModule> touched = new List<DamageModule>();

        public void Init(Vehicle v, float structure)
        {
            vehicle = v;
            MaxStructure = Structure = structure;
        }

        public DamageModule AddModule(ModuleType type, Transform space, Vector3 center, Vector3 size, float hp)
        {
            var m = new DamageModule { Type = type, Space = space, LocalBounds = new Bounds(center, size), MaxHp = hp, Hp = hp };
            Modules.Add(m);
            return m;
        }

        public DamageModule Get(ModuleType type)
        {
            for (int i = 0; i < Modules.Count; i++) if (Modules[i].Type == type) return Modules[i];
            return null;
        }

        public bool IsModuleDestroyed(ModuleType type)
        {
            var m = Get(type);
            return m != null && m.Destroyed;
        }

        public int CrewAlive()
        {
            int n = 0;
            foreach (var m in Modules) if (m.IsCrew && !m.Destroyed) n++;
            return n;
        }

        /// <summary>Shell penetrated armor at point travelling along dir. Returns a short human-readable damage summary.</summary>
        public string ApplyPenetration(Vector3 point, Vector3 dir, float damage, Vehicle attacker)
        {
            if (IsDestroyed) return "";
            LastAttacker = attacker;
            touched.Clear();
            dir = dir.normalized;
            TraceRay(point, dir, damage, 4.5f);
            for (int i = 0; i < 7; i++)
            {
                Vector3 axis = Vector3.Cross(dir, Random.onUnitSphere);
                if (axis.sqrMagnitude < 1e-4f) axis = Vector3.Cross(dir, Vector3.up);
                Vector3 spall = Quaternion.AngleAxis(Random.Range(6f, 24f), axis.normalized) * dir;
                TraceRay(point, spall, damage * 0.3f, 3.2f);
            }
            Structure -= damage * 0.55f;
            string summary = BuildSummary();
            Evaluate(attacker);
            Version++;
            return summary;
        }

        /// <summary>Direct hit on an external module such as a track.</summary>
        public string DamageModuleDirect(ModuleType type, float damage, Vehicle attacker)
        {
            if (IsDestroyed) return "";
            LastAttacker = attacker;
            touched.Clear();
            var m = Get(type);
            if (m != null) DamageModuleInternal(m, damage);
            string s = BuildSummary();
            Evaluate(attacker);
            Version++;
            return s;
        }

        public string ApplyBlast(Vector3 point, float radius, float damage, float hePenetration, Vehicle attacker)
        {
            if (IsDestroyed || radius <= 0f) return "";
            LastAttacker = attacker;
            touched.Clear();
            float roof = vehicle != null && vehicle.Spec != null ? vehicle.Spec.armor.hullTop : 20f;
            float overpressure = hePenetration >= roof ? 0.5f : 0.08f;
            foreach (var m in Modules)
            {
                if (m.Space == null) continue;
                Vector3 c = m.Space.TransformPoint(m.LocalBounds.center);
                float d = Vector3.Distance(point, c);
                if (d >= radius) continue;
                float f = 1f - d / radius;
                DamageModuleInternal(m, damage * f * (m.IsExternal ? 1.2f : overpressure));
            }
            float dc = Vector3.Distance(point, transform.position);
            Structure -= damage * 0.3f * overpressure * Mathf.Clamp01(1f - dc / (radius + 3f));
            string s = BuildSummary();
            Evaluate(attacker);
            Version++;
            return s;
        }

        void TraceRay(Vector3 origin, Vector3 dir, float damage, float depth)
        {
            foreach (var m in Modules)
            {
                if (m.Space == null || m.Destroyed) continue;
                Vector3 lo = m.Space.InverseTransformPoint(origin);
                Vector3 ld = m.Space.InverseTransformDirection(dir);
                if (m.LocalBounds.IntersectRay(new Ray(lo, ld), out float dist))
                {
                    dist = Mathf.Max(0f, dist);
                    if (dist > depth) continue;
                    DamageModuleInternal(m, damage * (1f - 0.5f * dist / depth));
                }
            }
        }

        void DamageModuleInternal(DamageModule m, float damage)
        {
            if (m.Destroyed || damage <= 0f) return;
            m.Hp = Mathf.Max(0f, m.Hp - damage);
            if (!touched.Contains(m)) touched.Add(m);
            if (m.Destroyed)
            {
                m.RepairCountdown = RepairSeconds;
                OnModuleDestroyed(m);
            }
            if (vehicle != null) vehicle.RefreshModuleEffects();
        }

        void OnModuleDestroyed(DamageModule m)
        {
            if (m.Type == ModuleType.Engine && Random.value < 0.4f) StartFire();
            if (m.Type == ModuleType.AmmoRack)
            {
                if (Random.value < 0.8f) pendingDetonation = true;
                else StartFire();
            }
        }

        string BuildSummary()
        {
            if (touched.Count == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < touched.Count; i++)
            {
                var m = touched[i];
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(m.DisplayName);
                if (m.Destroyed) sb.Append(m.IsCrew ? " knocked out" : " destroyed");
                else sb.Append(" damaged");
            }
            return sb.ToString();
        }

        void Evaluate(Vehicle attacker)
        {
            if (IsDestroyed) return;
            if (pendingDetonation) { Kill(attacker, true); return; }
            if (Structure <= 0f || CrewAlive() <= 1) Kill(attacker, false);
        }

        void StartFire()
        {
            if (OnFire || IsDestroyed) return;
            OnFire = true;
            fireTime = 12f;
            fireFx = EffectsManager.AttachFire(transform, new Vector3(0f, 0.6f, -1.5f));
            Version++;
        }

        void Update()
        {
            if (IsDestroyed) return;
            float dt = Time.deltaTime;
            if (OnFire)
            {
                Structure -= 16f * dt;
                fireTime -= dt;
                if (fireTime <= 0f)
                {
                    OnFire = false;
                    if (fireFx != null) { fireFx.Stop(true, ParticleSystemStopBehavior.StopEmitting); Destroy(fireFx.gameObject, 5f); fireFx = null; }
                    Version++;
                }
                if (Structure <= 0f) { Kill(LastAttacker, false); return; }
            }

            // Crew field repairs (only while enough crew are alive).
            if (CrewAlive() >= 2)
            {
                foreach (var m in Modules)
                {
                    if (!m.Destroyed || !m.Repairable) continue;
                    m.RepairCountdown -= dt;
                    if (m.RepairCountdown <= 0f)
                    {
                        m.Hp = m.MaxHp * 0.6f;
                        Version++;
                        if (vehicle != null) vehicle.RefreshModuleEffects();
                    }
                }
            }
        }

        public float ShortestRepairRemaining()
        {
            float best = -1f;
            foreach (var m in Modules)
                if (m.Destroyed && m.Repairable && (best < 0f || m.RepairCountdown < best)) best = m.RepairCountdown;
            return best;
        }

        public void Kill(Vehicle attacker, bool detonation)
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            AmmoDetonated = detonation;
            Structure = 0f;
            Version++;

            Vector3 pos = vehicle != null ? vehicle.CenterOfMassWorld : transform.position;
            EffectsManager.Explosion(pos + Vector3.up * 0.8f, detonation ? 2.2f : 1.2f);
            AudioManager.PlayExplosion(pos, detonation ? 1.0f : 0.8f);

            if (vehicle != null)
            {
                if (detonation && vehicle.TurretTransform != null)
                {
                    var t = vehicle.TurretTransform;
                    t.SetParent(null, true);
                    var rb = t.gameObject.AddComponent<Rigidbody>();
                    rb.mass = 3000f;
                    rb.AddForce(Vector3.up * rb.mass * 12f + Random.insideUnitSphere * rb.mass * 3f, ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * rb.mass * 4f, ForceMode.Impulse);
                }
                var burnt = MaterialLibrary.Burnt;
                if (vehicle.Renderers != null)
                    foreach (var r in vehicle.Renderers) if (r != null) r.sharedMaterial = burnt;
                vehicle.OnVehicleDestroyed();
            }
            if (fireFx != null) { fireFx.Stop(true, ParticleSystemStopBehavior.StopEmitting); Destroy(fireFx.gameObject, 5f); }
            EffectsManager.AttachWreckSmoke(transform);
            CombatEvents.ReportDestroyed(vehicle, attacker);
        }
    }
}

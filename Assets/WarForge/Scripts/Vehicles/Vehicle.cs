using System.Collections.Generic;
using UnityEngine;
using WarForge.Combat;
using WarForge.Data;

namespace WarForge.Vehicles
{
    public enum Team { Player, Enemy }

    /// <summary>Root component of every vehicle. Links all sub-systems together.</summary>
    public class Vehicle : MonoBehaviour
    {
        public static readonly List<Vehicle> All = new List<Vehicle>();

        public VehicleSpec Spec;
        public Team Team;
        public Rigidbody Body;
        public TankController Controller;
        public TurretController Turret;
        public TankGun Gun;
        public DamageModel Damage;
        public TankAudio Audio;
        public Transform TurretTransform;
        public Transform GunPivot;
        public Transform Muzzle;
        public Transform SightPoint;
        public Renderer[] Renderers;

        public bool IsAlive => Damage != null && !Damage.IsDestroyed;
        public string DisplayName => Spec != null ? Spec.displayName : name;
        public Vector3 CenterOfMassWorld => Body != null ? Body.worldCenterOfMass : transform.position;
        public Vector3 Velocity => Body != null ? Body.linearVelocity : Vector3.zero;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();

        /// <summary>Called by DamageModel whenever any module changes state.</summary>
        public void RefreshModuleEffects()
        {
            if (Damage == null) return;
            bool engine = Damage.IsModuleDestroyed(ModuleType.Engine);
            bool driver = Damage.IsModuleDestroyed(ModuleType.Driver);
            bool trackL = Damage.IsModuleDestroyed(ModuleType.TrackLeft);
            bool trackR = Damage.IsModuleDestroyed(ModuleType.TrackRight);
            bool breech = Damage.IsModuleDestroyed(ModuleType.Breech);
            bool loader = Damage.IsModuleDestroyed(ModuleType.Loader);
            bool gunner = Damage.IsModuleDestroyed(ModuleType.Gunner);
            bool drive = Damage.IsModuleDestroyed(ModuleType.TurretDrive);

            if (Controller != null)
            {
                Controller.DriveFactor = (engine || driver || trackL || trackR) ? 0f : 1f;
                Controller.TurnFactor = (engine || driver) ? 0f : ((trackL || trackR) ? 0.25f : 1f);
            }
            if (Gun != null)
            {
                Gun.BreechDisabled = breech;
                Gun.ReloadMultiplier = loader ? 1.7f : 1f;
            }
            if (Turret != null)
            {
                float m = 1f;
                if (drive) m *= 0.25f;
                if (gunner) m *= 0.5f;
                Turret.TraverseMultiplier = m;
            }
        }

        public void OnVehicleDestroyed()
        {
            if (Controller != null) { Controller.Throttle = 0f; Controller.Steer = 0f; Controller.DriveFactor = 0f; Controller.TurnFactor = 0f; }
            if (Gun != null) Gun.BreechDisabled = true;
            if (Turret != null) Turret.enabled = false;
            if (Audio != null) Audio.SetEngineRunning(false);
        }

        public void SetRenderersVisible(bool visible)
        {
            if (Renderers == null) return;
            foreach (var r in Renderers) if (r != null) r.enabled = visible;
        }
    }
}

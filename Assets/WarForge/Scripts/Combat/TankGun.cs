using System;
using UnityEngine;
using WarForge.Data;
using WarForge.FX;
using WarForge.Vehicles;

namespace WarForge.Combat
{
    public class TankGun : MonoBehaviour
    {
        public ShellSpec[] Shells;
        public float ReloadTime = 6f;
        public float ReloadMultiplier = 1f;
        public bool BreechDisabled;
        public float DispersionDeg = 0.08f;
        public event Action Fired;

        Vehicle vehicle;
        int index;
        float reloadTimer;

        public ShellSpec Current => Shells != null && Shells.Length > 0 ? Shells[index] : null;
        public int CurrentIndex => index;
        public float EffectiveReloadTime => ReloadTime * ReloadMultiplier;
        public bool Ready => reloadTimer <= 0f && !BreechDisabled;
        public float ReloadProgress => Mathf.Clamp01(1f - reloadTimer / Mathf.Max(0.01f, EffectiveReloadTime));
        public float ReloadRemaining => Mathf.Max(0f, reloadTimer);

        public void Init(Vehicle v, ShellSpec[] shells, float reload)
        {
            vehicle = v;
            Shells = shells;
            ReloadTime = reload;
            reloadTimer = 0.5f;
        }

        void Update()
        {
            if (reloadTimer > 0f) reloadTimer -= Time.deltaTime;
        }

        /// <summary>Switching ammunition requires a reload, like a real loader unloading the breech.</summary>
        public void SelectShell(int i)
        {
            if (Shells == null || Shells.Length == 0) return;
            i = Mathf.Clamp(i, 0, Shells.Length - 1);
            if (i == index) return;
            index = i;
            reloadTimer = EffectiveReloadTime;
        }

        public void CycleShell() => SelectShell(Shells == null || Shells.Length == 0 ? 0 : (index + 1) % Shells.Length);

        public bool TryFire()
        {
            if (!Ready || vehicle == null || vehicle.Muzzle == null || Current == null) return false;
            if (vehicle.Damage != null && vehicle.Damage.IsDestroyed) return false;

            Transform m = vehicle.Muzzle;
            Vector3 dir = Quaternion.Euler(UnityEngine.Random.Range(-DispersionDeg, DispersionDeg), UnityEngine.Random.Range(-DispersionDeg, DispersionDeg), 0f) * Vector3.forward;
            dir = m.TransformDirection(dir);
            Vector3 v = dir * Current.muzzleVelocity + vehicle.Velocity;
            ProjectilePool.Spawn(Current, vehicle, m.position, v);

            if (vehicle.Body != null)
                vehicle.Body.AddForceAtPosition(-m.forward * Current.caliberMm * 60f, m.position, ForceMode.Impulse);

            EffectsManager.MuzzleFlash(m.position, m.forward, Current.caliberMm / 75f);
            AudioManager.PlayCannon(m.position, Current.caliberMm, vehicle.Team == Team.Player);
            reloadTimer = EffectiveReloadTime;
            Fired?.Invoke();
            return true;
        }
    }
}

using UnityEngine;

namespace WarForge.Vehicles
{
    /// <summary>
    /// Independent turret yaw and gun elevation. Targets are world-space points; the turret rotates
    /// toward them at limited rates and reports whether the gun is on target.
    /// </summary>
    public class TurretController : MonoBehaviour
    {
        public Transform Turret;      // yaw pivot
        public Transform Gun;         // elevation pivot (child of Turret)
        public float TraverseDegPerSec = 28f;
        public float ElevationDegPerSec = 8f;
        public float MinElevation = -8f;
        public float MaxElevation = 20f;
        public float TraverseMultiplier = 1f;
        public bool Stabilized = true;

        public Vector3 AimPoint { get; private set; }
        public bool HasAim { get; private set; }
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public bool OnTarget { get; private set; }

        public void SetAimPoint(Vector3 world)
        {
            AimPoint = world;
            HasAim = true;
        }

        public void ClearAim() => HasAim = false;

        void LateUpdate()
        {
            if (!HasAim || Turret == null || Gun == null) return;
            float dt = Time.deltaTime;
            Transform hull = transform;

            Vector3 local = hull.InverseTransformPoint(AimPoint);
            float desiredYaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float yawStep = TraverseDegPerSec * TraverseMultiplier * dt;
            float dyaw = Mathf.DeltaAngle(Yaw, desiredYaw);
            Yaw += Mathf.Clamp(dyaw, -yawStep, yawStep);
            Turret.localRotation = Quaternion.Euler(0f, Yaw, 0f);

            Vector3 fromGun = Turret.InverseTransformPoint(AimPoint);
            float horiz = new Vector2(fromGun.x, fromGun.z).magnitude;
            float desiredPitch = -Mathf.Atan2(fromGun.y, Mathf.Max(0.01f, horiz)) * Mathf.Rad2Deg;
            desiredPitch = Mathf.Clamp(desiredPitch, MinElevation, MaxElevation);
            float pitchStep = ElevationDegPerSec * TraverseMultiplier * dt;
            Pitch = Mathf.MoveTowards(Pitch, desiredPitch, pitchStep);
            Gun.localRotation = Quaternion.Euler(Pitch, 0f, 0f);

            OnTarget = Mathf.Abs(dyaw) < 0.8f && Mathf.Abs(desiredPitch - Pitch) < 0.4f;
        }

        public void SnapTo(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = Mathf.Clamp(pitch, MinElevation, MaxElevation);
            if (Turret != null) Turret.localRotation = Quaternion.Euler(0f, Yaw, 0f);
            if (Gun != null) Gun.localRotation = Quaternion.Euler(Pitch, 0f, 0f);
        }

        /// <summary>World-space forward of the gun barrel.</summary>
        public Vector3 BarrelForward => Gun != null ? Gun.forward : transform.forward;
    }
}

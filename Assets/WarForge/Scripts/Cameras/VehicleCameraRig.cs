using UnityEngine;
using WarForge.Vehicles;

namespace WarForge.Cameras
{
    public enum CameraMode { ThirdPerson, GunnerSight }

    /// <summary>
    /// Player camera. Third person: spring-arm orbit behind the hull, turret aims at the screen centre.
    /// Gunner sight: locked to the sight point with magnified FOV; drag moves the aim direction and the turret slews to it.
    /// </summary>
    public class VehicleCameraRig : MonoBehaviour
    {
        public Camera Cam;
        public Vehicle Target;
        public CameraMode Mode = CameraMode.ThirdPerson;
        public float Sensitivity = 1f;
        public bool InvertY;

        public float ThirdDistance = 14f;
        public float GunnerFov = 14f;
        public float NormalFov = 60f;
        public float AimDistance = 400f;

        float orbitYaw;
        float orbitPitch = 12f;
        float gunnerYaw;
        float gunnerPitch;

        public Vector3 AimWorld { get; private set; }

        public void SetTarget(Vehicle v)
        {
            Target = v;
            gunnerYaw = 0f;
            gunnerPitch = 0f;
        }

        public void ToggleMode()
        {
            Mode = Mode == CameraMode.ThirdPerson ? CameraMode.GunnerSight : CameraMode.ThirdPerson;
            if (Mode == CameraMode.GunnerSight && Target != null && Target.Turret != null)
            {
                // Start the sight where the gun already points so the view does not snap.
                Vector3 f = Target.transform.InverseTransformDirection(Target.Turret.BarrelForward);
                gunnerYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                gunnerPitch = -Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg;
            }
        }

        /// <summary>Per-frame look delta in screen pixels from the touch / mouse layer.</summary>
        public void Look(Vector2 delta)
        {
            float invY = InvertY ? -1f : 1f;
            if (Mode == CameraMode.ThirdPerson)
            {
                orbitYaw += delta.x * 0.12f * Sensitivity;
                orbitPitch = Mathf.Clamp(orbitPitch - delta.y * 0.1f * Sensitivity * invY, -8f, 50f);
            }
            else
            {
                float k = 0.02f * Sensitivity / Mathf.Max(0.25f, GunnerFov / 60f);
                gunnerYaw += delta.x * k;
                gunnerPitch = Mathf.Clamp(gunnerPitch - delta.y * k * invY, -10f, 20f);
            }
        }

        void LateUpdate()
        {
            if (Target == null || Cam == null) return;
            Transform hull = Target.transform;
            Vector3 pivot = hull.position + Vector3.up * 2.2f;

            if (Mode == CameraMode.ThirdPerson)
            {
                Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, NormalFov, Time.deltaTime * 8f);
                Quaternion rot = Quaternion.Euler(orbitPitch, hull.eulerAngles.y + orbitYaw, 0f);
                Vector3 desired = pivot - rot * Vector3.forward * ThirdDistance;

                // Spring arm: never let the camera clip through terrain or buildings.
                if (Physics.Linecast(pivot, desired, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider.attachedRigidbody != Target.Body)
                    desired = hit.point + (pivot - hit.point).normalized * 0.5f;

                Cam.transform.position = Vector3.Lerp(Cam.transform.position, desired, 1f - Mathf.Exp(-14f * Time.deltaTime));
                Cam.transform.rotation = Quaternion.LookRotation(pivot + rot * Vector3.forward * 60f - Cam.transform.position, Vector3.up);

                // Turret aims at whatever is under the reticle.
                AimWorld = CrosshairWorldPoint();
                Target.Turret.SetAimPoint(AimWorld);
            }
            else
            {
                Transform sight = Target.SightPoint != null ? Target.SightPoint : hull;
                Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, GunnerFov, Time.deltaTime * 8f);
                Quaternion dirRot = hull.rotation * Quaternion.Euler(gunnerPitch, gunnerYaw, 0f);
                AimWorld = sight.position + dirRot * Vector3.forward * AimDistance;
                Cam.transform.position = sight.position;
                Cam.transform.rotation = dirRot;
                Target.Turret.SetAimPoint(AimWorld);
            }
        }

        public Vector3 CrosshairWorldPoint()
        {
            if (Cam == null || Target == null) return AimWorld;
            Ray r = Cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(r, out RaycastHit h, AimDistance, ~0, QueryTriggerInteraction.Ignore)
                && h.collider.attachedRigidbody != Target.Body)
                return h.point;
            return r.origin + r.direction * AimDistance;
        }
    }
}

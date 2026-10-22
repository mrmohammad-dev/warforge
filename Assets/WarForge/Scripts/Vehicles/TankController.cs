using UnityEngine;
using WarForge.Data;

namespace WarForge.Vehicles
{
    /// <summary>
    /// Raycast-suspension tank driving. Each track is sampled at several contact points; each point gets a spring,
    /// a longitudinal drive impulse (track speed target) and lateral grip. Steering comes from track differential,
    /// so tanks pivot, skid and fight slopes like tracked vehicles instead of cars.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TankController : MonoBehaviour
    {
        public const float Gravity = 9.81f;
        public float Throttle;   // -1..1 (player / AI)
        public float Steer;      // -1 left .. 1 right
        public float DriveFactor = 1f;  // 0 when engine, driver or both tracks are gone
        public float TurnFactor = 1f;

        public float ForwardSpeedMs { get; private set; }
        public float PowerRatio => Mathf.Clamp01(Mathf.Abs(Throttle));
        public bool Grounded { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;

        Rigidbody body;
        VehicleSpec spec;
        Vector3[] points;   // local-space contact positions for both tracks
        int[] side;         // -1 left, +1 right
        float springK, damping, massShare, halfTrack;
        const float RayOffset = 0.5f;
        const float RestLength = 0.8f;
        const float MaxTravel = 0.55f;
        const float SagTarget = 0.25f;

        public void Init(VehicleSpec s, Rigidbody rb, Vector3 hullSize)
        {
            spec = s;
            body = rb;
            body.mass = s.massKg;
            body.centerOfMass = new Vector3(0f, -0.4f, 0f);
            body.inertiaTensorRotation = Quaternion.identity;
            body.angularDamping = 0.8f;
            body.linearDamping = 0.05f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            int perSide = Mathf.Clamp(s.roadWheelsPerSide, 3, 8);
            int total = perSide * 2;
            points = new Vector3[total];
            side = new int[total];
            float trackX = hullSize.x * 0.5f - 0.25f;
            halfTrack = trackX;
            float len = hullSize.z * 0.8f;
            float y = -hullSize.y * 0.5f - 0.15f;
            for (int i = 0; i < perSide; i++)
            {
                float z = Mathf.Lerp(-len * 0.5f, len * 0.5f, perSide == 1 ? 0.5f : i / (float)(perSide - 1));
                points[i] = new Vector3(-trackX, y, z);
                points[i + perSide] = new Vector3(trackX, y, z);
                side[i] = -1;
                side[i + perSide] = 1;
            }
            massShare = s.massKg / total;
            springK = s.massKg * Gravity / (total * SagTarget);
            damping = springK * 0.12f;
        }

        public void SetSpeedLimits(VehicleSpec s) => spec = s;

        void FixedUpdate()
        {
            if (body == null || spec == null || points == null) return;
            float dt = Time.fixedDeltaTime;
            Vector3 up = transform.up;
            Vector3 fwd = transform.forward;
            int n = points.Length;
            Vector3 normalSum = Vector3.zero;
            int contacts = 0;
            var contactPoints = new Vector3[n];
            var contactNormals = new Vector3[n];
            var contactOk = new bool[n];

            // 1) Suspension.
            for (int i = 0; i < n; i++)
            {
                Vector3 local = points[i];
                Vector3 wp = transform.TransformPoint(local);
                Vector3 origin = wp + up * RayOffset;
                float maxDist = RestLength + MaxTravel;
                if (Physics.Raycast(origin, -up, out RaycastHit h, maxDist, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider != null && h.collider.attachedRigidbody != body)
                    {
                        float dist = h.distance;
                        float compression = RestLength - dist;
                        Vector3 pointVel = body.GetPointVelocity(wp);
                        float vUp = Vector3.Dot(pointVel, h.normal);
                        float force = springK * compression - damping * vUp;
                        if (force > 0f)
                        {
                            body.AddForceAtPosition(h.normal * force * dt, wp, ForceMode.Impulse);
                        }
                        contactOk[i] = true;
                        contactPoints[i] = h.point;
                        contactNormals[i] = h.normal;
                        normalSum += h.normal;
                        contacts++;
                    }
                }
            }

            Grounded = contacts > 0;
            GroundNormal = contacts > 0 ? (normalSum / contacts).normalized : Vector3.up;
            Vector3 driveFwd = Vector3.ProjectOnPlane(fwd, GroundNormal);
            if (driveFwd.sqrMagnitude < 1e-4f) driveFwd = fwd;
            driveFwd.Normalize();
            Vector3 right = Vector3.Cross(GroundNormal, driveFwd).normalized;
            ForwardSpeedMs = Vector3.Dot(body.linearVelocity, driveFwd);

            if (!Grounded) return;

            // 2) Drive: target track speed per side, applied as clamped impulses (stable, no overshoot).
            float throttle = Mathf.Clamp(Throttle, -1f, 1f) * DriveFactor;
            float steer = Mathf.Clamp(Steer, -1f, 1f) * TurnFactor;
            float maxF = spec.MaxForwardMs;
            float maxR = spec.MaxReverseMs;
            float target = throttle >= 0f ? throttle * maxF : throttle * maxR;
            float pivotSpeed = Mathf.Abs(steer) * spec.pivotTurnRateDeg * Mathf.Deg2Rad * halfTrack;
            float powerW = spec.enginePowerHp * 745.7f * DriveFactor;
            float speed = Mathf.Abs(ForwardSpeedMs);
            float tractionMax = spec.massKg * Gravity * 0.9f;
            float totalForce = Mathf.Min(powerW / Mathf.Max(2f, speed), tractionMax);

            for (int i = 0; i < n; i++)
            {
                if (!contactOk[i]) continue;
                Vector3 wp = contactPoints[i];
                Vector3 v = body.GetPointVelocity(wp);
                float vLong = Vector3.Dot(v, driveFwd);
                float vLat = Vector3.Dot(v, right);

                // Steering: outer track faster when turning toward the other side.
                float sideSpeed = target - side[i] * steer * pivotSpeed;
                if (Mathf.Abs(throttle) < 0.05f) sideSpeed = -side[i] * steer * pivotSpeed; // pivot in place
                float per = totalForce / n;
                float vt = sideSpeed;
                float impulseLong = Mathf.Clamp((vt - vLong) * massShare * 0.35f, -per * dt, per * dt);
                if (Mathf.Abs(throttle) < 0.02f && Mathf.Abs(steer) < 0.02f)
                {
                    // Service brake: bleed longitudinal speed.
                    impulseLong = Mathf.Clamp(-vLong * massShare * 0.25f, -tractionMax * dt / n, tractionMax * dt / n);
                }
                body.AddForceAtPosition(driveFwd * impulseLong, wp, ForceMode.Impulse);

                // 3) Lateral grip (tracks do not slide sideways).
                float lat = Mathf.Clamp(-vLat * massShare * 0.45f, -tractionMax * dt / n, tractionMax * dt / n);
                body.AddForceAtPosition(right * lat, wp, ForceMode.Impulse);
            }
        }

        public void ResetForNewSpawn()
        {
            Throttle = 0f;
            Steer = 0f;
        }
    }
}

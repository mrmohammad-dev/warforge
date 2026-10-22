using UnityEngine;

namespace WarForge.Combat
{
    public enum HitOutcome { Ricochet, NonPenetration, Penetration }

    /// <summary>Pure armor math, no scene dependencies (unit tested).</summary>
    public static class ArmorMath
    {
        public const float DefaultRicochetAngle = 70f;
        public const float MaxEffectiveAngle = 85f;

        /// <summary>Angle between the incoming shell and the plate normal. 0 = perfectly perpendicular hit.</summary>
        public static float ImpactAngleDeg(Vector3 shellDirection, Vector3 surfaceNormal)
        {
            if (shellDirection.sqrMagnitude < 1e-8f || surfaceNormal.sqrMagnitude < 1e-8f) return 0f;
            float c = Vector3.Dot(-shellDirection.normalized, surfaceNormal.normalized);
            return Mathf.Acos(Mathf.Clamp(c, -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>Line-of-sight thickness: thickness / cos(angle), with optional shell normalization.</summary>
        public static float EffectiveThickness(float thickness, float angleDeg, float normalizationDeg = 0f)
        {
            float a = Mathf.Clamp(angleDeg - Mathf.Max(0f, normalizationDeg), 0f, MaxEffectiveAngle);
            return thickness / Mathf.Cos(a * Mathf.Deg2Rad);
        }

        public static HitOutcome Evaluate(float penetration, float thickness, float angleDeg, float ricochetAngleDeg, float normalizationDeg, out float effectiveThickness)
        {
            effectiveThickness = EffectiveThickness(thickness, angleDeg, normalizationDeg);
            if (angleDeg >= ricochetAngleDeg) return HitOutcome.Ricochet;
            return penetration >= effectiveThickness ? HitOutcome.Penetration : HitOutcome.NonPenetration;
        }
    }

    /// <summary>Pure projectile math for aiming (unit tested).</summary>
    public static class BallisticsMath
    {
        /// <summary>Low-arc launch angle (radians, above horizontal) to hit a point at horizontal distance x and height y.</summary>
        public static bool SolveLowAngle(float speed, float gravity, float x, float y, out float angleRad)
        {
            angleRad = 0f;
            if (speed <= 0f) return false;
            if (x < 0.001f)
            {
                angleRad = y >= 0f ? Mathf.PI * 0.5f : -Mathf.PI * 0.5f;
                return true;
            }
            if (gravity <= 1e-5f)
            {
                angleRad = Mathf.Atan2(y, x);
                return true;
            }
            float v2 = speed * speed;
            float disc = v2 * v2 - gravity * (gravity * x * x + 2f * y * v2);
            if (disc < 0f)
            {
                angleRad = Mathf.PI * 0.25f; // out of range: best effort 45 degrees
                return false;
            }
            angleRad = Mathf.Atan((v2 - Mathf.Sqrt(disc)) / (gravity * x));
            return true;
        }

        public static float FlightTime(float speed, float angleRad, float horizontalDistance)
        {
            float vx = speed * Mathf.Cos(angleRad);
            return vx > 1e-4f ? horizontalDistance / vx : 0f;
        }

        public static Vector3 PositionAt(Vector3 p0, Vector3 v0, Vector3 g, float t) => p0 + v0 * t + 0.5f * t * t * g;

        /// <summary>
        /// Returns a world point that, when the gun points straight at it, makes a shell hit the (moving) target.
        /// Compensates for gravity drop and target motion (lead).
        /// </summary>
        public static Vector3 ComputeAimPoint(Vector3 muzzle, Vector3 target, Vector3 targetVelocity, float speed, float gravity, int iterations = 3)
        {
            Vector3 predicted = target;
            float angle;
            for (int i = 0; i < iterations; i++)
            {
                Vector3 d = predicted - muzzle;
                float horiz = new Vector2(d.x, d.z).magnitude;
                if (horiz < 0.01f) return predicted;
                SolveLowAngle(speed, gravity, horiz, d.y, out angle);
                float t = FlightTime(speed, angle, horiz);
                predicted = target + targetVelocity * t;
            }
            Vector3 final = predicted - muzzle;
            float h = new Vector2(final.x, final.z).magnitude;
            if (h < 0.01f) return predicted;
            SolveLowAngle(speed, gravity, h, final.y, out angle);
            Vector3 flat = new Vector3(final.x, 0f, final.z) / h;
            Vector3 dir = flat * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
            return muzzle + dir * final.magnitude;
        }
    }
}

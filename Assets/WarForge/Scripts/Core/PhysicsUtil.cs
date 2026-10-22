using UnityEngine;

namespace WarForge.Core
{
    public static class PhysicsUtil
    {
        static readonly RaycastHit[] Buffer = new RaycastHit[32];

        /// <summary>Nearest non-trigger hit along a ray that does not belong to the ignored rigidbody.</summary>
        public static bool Raycast(Vector3 origin, Vector3 direction, float distance, Rigidbody ignore, out RaycastHit result, Collider ignoreCollider = null)
        {
            result = default;
            if (direction.sqrMagnitude < 1e-8f || distance <= 0f) return false;
            int n = Physics.RaycastNonAlloc(origin, direction.normalized, Buffer, distance, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = Buffer[i];
                if (h.collider == null) continue;
                if (ignoreCollider != null && h.collider == ignoreCollider) continue;
                if (ignore != null && h.collider.attachedRigidbody == ignore) continue;
                if (h.distance < best)
                {
                    best = h.distance;
                    result = h;
                    found = true;
                }
            }
            return found;
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, float distance, Rigidbody ignore, out RaycastHit result)
        {
            result = default;
            if (direction.sqrMagnitude < 1e-8f) return false;
            int n = Physics.SphereCastNonAlloc(origin, radius, direction.normalized, Buffer, distance, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = Buffer[i];
                if (h.collider == null) continue;
                if (ignore != null && h.collider.attachedRigidbody == ignore) continue;
                if (h.distance <= 0f && h.point == Vector3.zero) continue; // overlapping at start
                if (h.distance < best)
                {
                    best = h.distance;
                    result = h;
                    found = true;
                }
            }
            return found;
        }
    }
}

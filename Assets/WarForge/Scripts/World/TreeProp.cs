using UnityEngine;
using WarForge.FX;

namespace WarForge.World
{
    /// <summary>Tree that topples when rammed by a tank or hit by a shell.</summary>
    public class TreeProp : MonoBehaviour
    {
        bool fallen;

        void OnCollisionEnter(Collision c)
        {
            if (fallen || c.rigidbody == null || c.rigidbody.mass < 5000f) return;
            if (c.relativeVelocity.magnitude < 1.5f) return;
            Vector3 p = c.contactCount > 0 ? c.GetContact(0).point : transform.position;
            Topple(c.rigidbody.linearVelocity * 1000f, p);
        }

        public void Topple(Vector3 impulse, Vector3 point)
        {
            if (fallen) return;
            fallen = true;
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 350f;
            rb.angularDamping = 0.5f;
            Vector3 push = impulse;
            push.y = 0f;
            if (push.sqrMagnitude < 1f) push = UnityEngine.Random.insideUnitSphere * 1500f;
            rb.AddForceAtPosition(push.normalized * 1800f, transform.position + Vector3.up * 4f, ForceMode.Impulse);
            AudioManager.PlayTreeCrack(transform.position);
        }
    }
}

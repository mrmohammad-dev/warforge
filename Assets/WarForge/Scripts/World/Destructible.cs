using System;
using UnityEngine;
using WarForge.FX;

namespace WarForge.World
{
    /// <summary>
    /// Breakable prop (house, wall, bunker). On destruction it is replaced by physics debris and a rubble pile.
    /// The collider must live on the same GameObject so tank collisions reach OnCollisionEnter.
    /// </summary>
    public class Destructible : MonoBehaviour
    {
        public float MaxHp = 400f;
        public float Hp = 400f;
        public bool Crushable;
        public float CrushImpulse = 3f;      // relative speed (m/s) needed for a heavy vehicle to smash it
        public int DebrisCount = 10;
        public Color DebrisColor = new Color(0.55f, 0.5f, 0.45f);
        public bool LeaveRubble = true;
        public bool IsTarget;                // mission objective target
        public string DisplayName = "Structure";
        public event Action<Destructible> Broken;

        bool broken;
        public bool IsBroken => broken;

        public void ApplyDamage(float damage, Vector3 point, Vector3 impulse)
        {
            if (broken || damage <= 0f) return;
            Hp -= damage;
            if (Hp <= 0f) Break(point, impulse);
            else EffectsManager.Dust(point, 0.6f);
        }

        void OnCollisionEnter(Collision c)
        {
            if (!Crushable || broken || c.rigidbody == null) return;
            if (c.rigidbody.mass < 5000f) return;
            if (c.relativeVelocity.magnitude < CrushImpulse) return;
            Vector3 p = c.contactCount > 0 ? c.GetContact(0).point : transform.position;
            Break(p, c.relativeVelocity * 200f);
        }

        public void Break(Vector3 point, Vector3 impulse)
        {
            if (broken) return;
            broken = true;
            Bounds b = ComputeBounds();
            EffectsManager.Dust(b.center, Mathf.Clamp(b.size.magnitude / 6f, 0.8f, 3f));
            AudioManager.PlayCollapse(b.center);
            var mat = MaterialLibrary.Lit(DebrisColor, 0.1f);
            int count = Mathf.Clamp(DebrisCount, 0, 24);
            for (int i = 0; i < count; i++)
            {
                var d = GameObject.CreatePrimitive(PrimitiveType.Cube);
                d.name = "Debris";
                float s = Mathf.Clamp(b.size.magnitude * 0.08f, 0.25f, 1.4f) * UnityEngine.Random.Range(0.5f, 1.2f);
                d.transform.localScale = new Vector3(s, s * UnityEngine.Random.Range(0.4f, 1f), s * UnityEngine.Random.Range(0.6f, 1.3f));
                d.transform.position = new Vector3(
                    UnityEngine.Random.Range(b.min.x, b.max.x),
                    UnityEngine.Random.Range(b.min.y, b.center.y + b.extents.y * 0.6f),
                    UnityEngine.Random.Range(b.min.z, b.max.z));
                d.transform.rotation = UnityEngine.Random.rotation;
                d.GetComponent<Renderer>().sharedMaterial = mat;
                var rb = d.AddComponent<Rigidbody>();
                rb.mass = 40f * s;
                rb.AddForce((impulse.normalized * 4f + UnityEngine.Random.insideUnitSphere * 3f + Vector3.up * 2f) * rb.mass, ForceMode.Impulse);
                Destroy(d, UnityEngine.Random.Range(6f, 9f));
            }
            if (LeaveRubble && b.size.x > 1.5f)
            {
                var rubble = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rubble.name = name + "_Rubble";
                rubble.transform.position = new Vector3(b.center.x, b.min.y + 0.3f, b.center.z);
                rubble.transform.localScale = new Vector3(b.size.x * 0.9f, 0.8f, b.size.z * 0.9f);
                rubble.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, UnityEngine.Random.Range(-4f, 4f));
                rubble.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Lit(DebrisColor * 0.8f, 0.05f);
            }
            Broken?.Invoke(this);
            Destroy(gameObject);
        }

        Bounds ComputeBounds()
        {
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(transform.position, Vector3.one * 2f);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}

using UnityEngine;

namespace WarForge.FX
{
    /// <summary>
    /// Lightweight effects built from code (no prefabs needed). Keep effect counts small for mobile.
    /// </summary>
    public static class EffectsManager
    {
        static Transform root;

        static Transform Root
        {
            get
            {
                if (root == null) root = new GameObject("FX").transform;
                return root;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => root = null;

        static ParticleSystem Make(string name, Vector3 pos, Color color, float life, float startSize, float speed, int burst, float rate = 0f, float lifetimeDuration = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MaterialLibrary.Particle(color);
            var main = ps.main;
            main.duration = lifetimeDuration;
            main.loop = false;
            main.startLifetime = life;
            main.startSize = startSize;
            main.startSpeed = speed;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.maxParticles = 200;
            var em = ps.emission;
            em.rateOverTime = rate;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
            ps.Play();
            return ps;
        }

        public static void Explosion(Vector3 pos, float scale)
        {
            scale = Mathf.Clamp(scale, 0.5f, 3f);
            Make("Explosion", pos, new Color(1f, 0.6f, 0.2f), 0.8f * scale, 1.2f * scale, 6f * scale, 40, 0f, 0.3f);
            Make("ExplosionSmoke", pos, new Color(0.25f, 0.23f, 0.22f, 0.8f), 3f, 2.5f * scale, 2f, 18, 0f, 0.5f);
            var light = new GameObject("BlastLight").AddComponent<Light>();
            light.transform.position = pos + Vector3.up;
            light.type = LightType.Point; light.color = new Color(1f, 0.6f, 0.3f);
            light.intensity = 8f * scale; light.range = 12f * scale;
            Object.Destroy(light.gameObject, 0.25f);
        }

        public static void Dust(Vector3 pos, float scale)
        {
            Make("Dust", pos, new Color(0.6f, 0.55f, 0.45f, 0.7f), 1.6f, 1.2f * scale, 2f, Mathf.RoundToInt(10 * scale), 0f, 0.3f);
        }

        public static void Sparks(Vector3 pos, Vector3 normal)
        {
            var ps = Make("Sparks", pos + normal * 0.05f, new Color(1f, 0.85f, 0.4f), 0.35f, 0.05f, 5f, 14, 0f, 0.1f);
            ps.transform.rotation = Quaternion.LookRotation(normal);
        }

        public static void Penetration(Vector3 pos, Vector3 dir)
        {
            Make("Spall", pos, new Color(1f, 0.7f, 0.3f), 0.6f, 0.08f, 6f, 20, 0f, 0.15f);
        }

        public static void GroundImpact(Vector3 pos, Vector3 normal, float scale)
        {
            Dust(pos + normal * 0.1f, Mathf.Clamp(scale, 0.6f, 2.2f));
        }

        public static void MuzzleFlash(Vector3 pos, Vector3 forward, float scale)
        {
            var ps = Make("Muzzle", pos, new Color(1f, 0.8f, 0.4f), 0.15f, 0.9f * scale, 0.5f, 8, 0f, 0.1f);
            ps.transform.rotation = Quaternion.LookRotation(forward);
            Make("MuzzleSmoke", pos + forward * 0.5f, new Color(0.6f, 0.6f, 0.6f, 0.6f), 2.5f, 1.2f * scale, 1.5f, 8, 0f, 0.2f)
                .transform.rotation = Quaternion.LookRotation(forward);
        }

        public static void Tracer(Vector3 from, Vector3 to)
        {
            // Visual trace is handled by TrailRenderer on the projectile (CreateTracer).
        }

        public static TrailRenderer CreateTracer(GameObject go, bool he)
        {
            var tr = go.GetComponent<TrailRenderer>();
            if (tr == null) tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.35f;
            tr.startWidth = he ? 0.25f : 0.12f;
            tr.endWidth = 0f;
            tr.material = MaterialLibrary.Unlit(he ? new Color(1f, 0.5f, 0.2f) : new Color(1f, 0.95f, 0.7f));
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            return tr;
        }

        public static ParticleSystem AttachFire(Transform parent, Vector3 localOffset)
        {
            var ps = Make("Fire", parent.position, new Color(1f, 0.45f, 0.1f, 0.9f), 0.9f, 0.6f, 0.8f, 0, 25f, 60f);
            ps.transform.SetParent(parent, true);
            ps.transform.localPosition = localOffset;
            return ps;
        }

        public static void AttachWreckSmoke(Transform parent)
        {
            var ps = Make("WreckSmoke", parent.position + Vector3.up, new Color(0.15f, 0.14f, 0.13f, 0.7f), 4f, 2f, 0.6f, 0, 6f, 120f);
            ps.transform.SetParent(parent, true);
        }
    }
}

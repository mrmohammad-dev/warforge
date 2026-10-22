using UnityEngine;

namespace WarForge.FX
{
    /// <summary>
    /// Procedural placeholder audio (generated at runtime, no sound files). Replace with authored clips later
    /// by assigning them to the fields below from a ScriptableObject.
    /// </summary>
    public static class AudioManager
    {
        static AudioClip cannon, explosion, impact, ricochet, engine, collapse, crack;
        static AudioSource oneShot;
        static Transform root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            cannon = explosion = impact = ricochet = engine = collapse = crack = null;
            oneShot = null; root = null;
        }

        static AudioSource Source
        {
            get
            {
                if (oneShot == null)
                {
                    root = new GameObject("AudioOneShots").transform;
                    oneShot = root.gameObject.AddComponent<AudioSource>();
                    oneShot.spatialBlend = 0f;
                    oneShot.playOnAwake = false;
                }
                return oneShot;
            }
        }

        public static AudioClip Cannon => cannon ??= Synth("cannon", 0.9f, (t, d) => Noise(t, d) * Mathf.Exp(-t * 9f) + Mathf.Sin(t * 60f * Mathf.PI) * Mathf.Exp(-t * 14f) * 0.6f);
        public static AudioClip Explosion => explosion ??= Synth("explosion", 1.6f, (t, d) => Noise(t, d) * Mathf.Exp(-t * 3.2f) * 1.2f);
        public static AudioClip Impact => impact ??= Synth("impact", 0.35f, (t, d) => Noise(t, d) * Mathf.Exp(-t * 18f));
        public static AudioClip Ricochet => ricochet ??= Synth("ricochet", 0.5f, (t, d) => Mathf.Sin(t * 2400f * Mathf.PI * (1f - t * 1.2f)) * Mathf.Exp(-t * 9f) * 0.5f);
        public static AudioClip Collapse => collapse ??= Synth("collapse", 1.8f, (t, d) => Noise(t, d) * Mathf.Exp(-t * 2.2f) * (0.6f + 0.4f * Mathf.Sin(t * 9f)));
        public static AudioClip TreeCrack => crack ??= Synth("crack", 0.6f, (t, d) => Noise(t, d) * Mathf.Exp(-t * 12f));
        public static AudioClip Engine => engine ??= Loop("engine", 2f, 38f);

        static float Noise(float t, float d) => Random.Range(-1f, 1f);

        public static void PlayCannon(Vector3 p, float caliber, bool own) => PlayAt(Cannon, p, Mathf.Clamp(caliber / 90f, 0.5f, 1.2f) * (own ? 1f : 0.7f));
        public static void PlayExplosion(Vector3 p, float v) => PlayAt(Explosion, p, v);
        public static void PlayImpact(Vector3 p, bool metal) => PlayAt(metal ? Impact : Impact, p, 0.7f);
        public static void PlayRicochet(Vector3 p) => PlayAt(Ricochet, p, 0.6f);
        public static void PlayCollapse(Vector3 p) => PlayAt(Collapse, p, 0.9f);
        public static void PlayTreeCrack(Vector3 p) => PlayAt(TreeCrack, p, 0.7f);

        static void PlayAt(AudioClip clip, Vector3 p, float volume)
        {
            if (clip == null) return;
            // Spatial mixing with AudioSource.PlayClipAtPoint is cheap enough for short one-shots.
            AudioSource.PlayClipAtPoint(clip, p, Mathf.Clamp01(volume) * AudioListener.volume);
        }

        static AudioClip Synth(string name, float seconds, System.Func<float, float, float> f)
        {
            int rate = 22050;
            int n = Mathf.Max(1, (int)(seconds * rate));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                data[i] = Mathf.Clamp(f(t, seconds), -1f, 1f) * 0.8f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Loop(string name, float seconds, float freq)
        {
            int rate = 22050;
            int n = (int)(seconds * rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float s = 0.4f * Mathf.Sin(2f * Mathf.PI * freq * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t) + 0.15f * (Random.value - 0.5f);
                data[i] = s * 0.5f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            clip.name = name;
            return clip;
        }
    }
}

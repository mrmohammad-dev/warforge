using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace WarForge.FX
{
    /// <summary>Creates materials at runtime so the project needs no binary texture or material assets to run.</summary>
    public static class MaterialLibrary
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Shader litShader;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cache.Clear();

        static Shader Lit
        {
            get
            {
                if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader == null) litShader = Shader.Find("Standard");
                return litShader;
            }
        }

        public static Material Lit(Color color, float metallic = 0f, float smoothness = 0.25f)
        {
            string key = $"lit_{color.r:F2}_{color.g:F2}_{color.b:F2}_{metallic:F2}_{smoothness:F2}";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Lit) { name = key, color = color };
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            cache[key] = m;
            return m;
        }

        public static Material Unlit(Color color)
        {
            string key = "unlit_" + color.ToString();
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            m = new Material(sh) { name = key, color = color };
            cache[key] = m;
            return m;
        }

        public static Material Particle(Color color)
        {
            string key = "part_" + color.ToString();
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            m = new Material(sh) { name = key, color = color };
            cache[key] = m;
            return m;
        }

        public static Material Burnt => Lit(new Color(0.08f, 0.07f, 0.06f), 0.1f, 0.05f);
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace WarForge.FX
{
    /// <summary>
    /// Creates materials at runtime. Shader selection follows the active render pipeline:
    /// URP shaders are preferred when URP is configured; built-in shaders are preferred otherwise.
    /// </summary>
    public static class MaterialLibrary
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Shader litShader;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            cache.Clear();
            litShader = null;
        }

        static bool UsesScriptableRenderPipeline =>
            UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;

        static Shader LitShader
        {
            get
            {
                if (litShader != null) return litShader;

                // This repository currently has no committed URP pipeline asset.
                // Do not select URP/Lit while the project is actually using the built-in pipeline.
                if (UsesScriptableRenderPipeline)
                {
                    litShader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Standard");
                }
                else
                {
                    litShader = Shader.Find("Standard")
                        ?? Shader.Find("Universal Render Pipeline/Lit");
                }

                return litShader;
            }
        }

        public static Material Lit(Color color, float metallic = 0f, float smoothness = 0.25f)
        {
            string key = $"lit_{color.r:R}_{color.g:R}_{color.b:R}_{color.a:R}_{metallic:R}_{smoothness:R}";
            if (cache.TryGetValue(key, out var m) && m != null) return m;

            var shader = LitShader;
            if (shader == null)
            {
                Debug.LogError("[WarForge] Neither a compatible Lit shader nor the Standard shader could be found.");
                return null;
            }

            m = new Material(shader) { name = key, color = color };
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            cache[key] = m;
            return m;
        }

        public static Material Unlit(Color color)
        {
            string key = "unlit_" + color;
            if (cache.TryGetValue(key, out var m) && m != null) return m;

            Shader shader;
            if (UsesScriptableRenderPipeline)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Unlit/Color")
                    ?? Shader.Find("Sprites/Default");
            }
            else
            {
                shader = Shader.Find("Unlit/Color")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                Debug.LogError("[WarForge] No supported unlit shader could be found.");
                return null;
            }

            m = new Material(shader) { name = key, color = color };
            cache[key] = m;
            return m;
        }

        public static Material Particle(Color color)
        {
            string key = "part_" + color;
            if (cache.TryGetValue(key, out var m) && m != null) return m;

            Shader shader;
            if (UsesScriptableRenderPipeline)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                    ?? Shader.Find("Particles/Standard Unlit")
                    ?? Shader.Find("Sprites/Default");
            }
            else
            {
                shader = Shader.Find("Particles/Standard Unlit")
                    ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }

            if (shader == null)
            {
                Debug.LogError("[WarForge] No supported particle shader could be found.");
                return null;
            }

            m = new Material(shader) { name = key, color = color };
            cache[key] = m;
            return m;
        }

        public static Material Burnt => Lit(new Color(0.08f, 0.07f, 0.06f), 0.1f, 0.05f);
    }
}

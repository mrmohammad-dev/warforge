using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WarForge.Core
{
    public static class SettingsApplier
    {
        public static readonly string[] QualityNames = { "LOW", "MEDIUM", "HIGH" };

        public static void Apply(SettingsData s)
        {
            if (s == null) return;
            AudioListener.volume = s.masterVolume;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = s.quality == 0 ? 30 : 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Only touch the URP asset in player builds; in the Editor this would permanently modify the asset file.
            if (!Application.isEditor && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.renderScale = s.quality == 0 ? 0.7f : (s.quality == 1 ? 0.85f : 1f);
                urp.shadowDistance = s.quality == 0 ? 50f : (s.quality == 1 ? 90f : 140f);
            }
        }

        public static void ApplyTerrain(Terrain terrain, SettingsData s)
        {
            if (terrain == null || s == null) return;
            terrain.heightmapPixelError = s.quality == 0 ? 14f : (s.quality == 1 ? 8f : 4f);
            terrain.basemapDistance = s.quality == 0 ? 80f : 200f;
        }
    }
}

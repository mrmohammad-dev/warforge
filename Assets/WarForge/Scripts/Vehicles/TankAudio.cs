using UnityEngine;
using WarForge.FX;

namespace WarForge.Vehicles
{
    [RequireComponent(typeof(AudioSource))]
    public class TankAudio : MonoBehaviour
    {
        AudioSource engine;
        TankController controller;
        bool running = true;

        public void Init(TankController c)
        {
            controller = c;
            engine = GetComponent<AudioSource>();
            engine.clip = AudioManager.Engine;
            engine.loop = true;
            engine.spatialBlend = 1f;
            engine.maxDistance = 120f;
            engine.rolloffMode = AudioRolloffMode.Linear;
            engine.volume = 0.35f;
            engine.Play();
        }

        public void SetEngineRunning(bool on)
        {
            running = on;
            if (engine != null) engine.mute = !on;
        }

        void Update()
        {
            if (engine == null || controller == null || !running) return;
            float speed = Mathf.Abs(controller.ForwardSpeedMs);
            engine.pitch = Mathf.Lerp(0.7f, 1.6f, Mathf.Clamp01(speed / 15f) * 0.6f + controller.PowerRatio * 0.4f);
        }
    }
}

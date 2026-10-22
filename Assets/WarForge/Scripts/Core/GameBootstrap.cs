using UnityEngine;
using UnityEngine.SceneManagement;

namespace WarForge.Core
{
    /// <summary>
    /// Creates the game root automatically whenever the Main scene is loaded.
    /// The scene file therefore needs no serialized script references, which keeps it robust.
    /// </summary>
    public static class GameBootstrap
    {
        public const string MainSceneName = "Main";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnFirstSceneLoaded()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryCreateRoot(SceneManager.GetActiveScene());
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) TryCreateRoot(scene);
        }

        static void TryCreateRoot(Scene scene)
        {
            if (scene.name != MainSceneName) return;
            if (Object.FindFirstObjectByType<GameRoot>() != null) return;
            new GameObject("WarForge.GameRoot").AddComponent<GameRoot>();
        }
    }
}

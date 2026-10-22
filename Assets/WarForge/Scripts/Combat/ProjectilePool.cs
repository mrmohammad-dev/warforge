using System.Collections.Generic;
using UnityEngine;
using WarForge.Data;
using WarForge.Vehicles;

namespace WarForge.Combat
{
    public static class ProjectilePool
    {
        static readonly Stack<Projectile> Free = new Stack<Projectile>();
        static Transform root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Free.Clear(); root = null; }

        public static Projectile Spawn(ShellSpec shell, Vehicle owner, Vector3 position, Vector3 velocity)
        {
            if (root == null)
            {
                root = new GameObject("Projectiles").transform;
                Free.Clear(); // previous scene's pooled objects are gone
            }
            Projectile p = null;
            while (Free.Count > 0 && p == null) p = Free.Pop();
            if (p == null)
            {
                var go = new GameObject("Shell");
                go.transform.SetParent(root, false);
                p = go.AddComponent<Projectile>();
            }
            p.gameObject.SetActive(true);
            p.Launch(shell, owner, position, velocity);
            return p;
        }

        public static void Release(Projectile p)
        {
            if (p == null) return;
            p.gameObject.SetActive(false);
            Free.Push(p);
        }
    }
}

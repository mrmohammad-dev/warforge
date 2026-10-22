using System.Collections.Generic;
using UnityEngine;
using WarForge.FX;
using WarForge.Missions;

namespace WarForge.World
{
    /// <summary>
    /// Procedurally builds a test arena: heightfield terrain, a village of breakable houses, tree lines and walls.
    /// Everything is generated from the mission seed, so restarts are identical. Replace pieces with authored
    /// prefabs or terrain assets as the art pipeline matures.
    /// </summary>
    public static class ArenaBuilder
    {
        public const float Size = 800f;

        public struct Result
        {
            public Vector3 PlayerSpawn;
            public Vector3 PlayerSpawnForward;
            public List<Vector3> EnemySpawns;
            public List<Vector3> EnemyForwards;
            public Vector3[] PatrolPoints;
        }

        public static Result Build(MissionDefinition m, Transform parent)
        {
            var root = new GameObject("Arena").transform;
            root.SetParent(parent, false);
            var result = new Result { EnemySpawns = new List<Vector3>(), EnemyForwards = new List<Vector3>() };
            Random.InitState(m.Seed);

            var terrain = BuildTerrain(root, m.Seed);
            float Sample(float x, float z) => terrain.SampleHeight(new Vector3(x, 0f, z)); // SampleHeight takes world XZ and returns world Y

            // Player spawn in the south-west, enemies at mission-defined positions.
            result.PlayerSpawn = new Vector3(-40f, Sample(-40f, -40f) + 1.2f, -40f);
            result.PlayerSpawnForward = new Vector3(0.7f, 0f, 0.7f).normalized;

            foreach (var e in m.Enemies)
            {
                Vector3 p = new Vector3(e.X, Sample(e.X, e.Z) + 1.2f, e.Z);
                result.EnemySpawns.Add(p);
                result.EnemyForwards.Add((new Vector3(0f, 0f, 0f) - new Vector3(p.x, 0f, p.z)).normalized);
            }

            result.PatrolPoints = new Vector3[m.PatrolLoop.Length];
            for (int i = 0; i < m.PatrolLoop.Length; i++)
            {
                var (x, z) = m.PatrolLoop[i];
                result.PatrolPoints[i] = new Vector3(x, Sample(x, z) + 0.5f, z);
            }

            // Village in the middle: destructible houses.
            var village = new GameObject("Village").transform;
            village.SetParent(root, false);
            for (int i = 0; i < 9; i++)
            {
                float x = -60f + (i % 3) * 60f + Random.Range(-6f, 6f);
                float z = -40f + (i / 3) * 60f + Random.Range(-6f, 6f);
                if (Mathf.Abs(x) < 8f && Mathf.Abs(z) < 8f) continue; // keep the middle clear
                CreateHouse(village, new Vector3(x, Sample(x, z), z), Random.Range(0f, 360f));
            }

            // Walls (breakable low concrete) along ridge.
            for (int i = 0; i < 6; i++)
            {
                float x = -150f + i * 40f;
                float z = 30f;
                CreateWall(root, new Vector3(x, Sample(x, z), z), 36f);
            }

            // Tree lines and scattered forest.
            var forest = new GameObject("Forest").transform;
            forest.SetParent(root, false);
            for (int i = 0; i < 260; i++)
            {
                float x = Random.Range(-Size * 0.45f, Size * 0.45f);
                float z = Random.Range(-Size * 0.45f, Size * 0.45f);
                if (Mathf.Abs(x) < 30f && Mathf.Abs(z) < 30f) continue;
                if (new Vector2(x - result.PlayerSpawn.x, z - result.PlayerSpawn.z).magnitude < 12f) continue;
                CreateTree(forest, new Vector3(x, Sample(x, z), z), Random.Range(0.8f, 1.4f));
            }

            return result;
        }

        static Terrain BuildTerrain(Transform parent, int seed)
        {
            const int res = 257;
            var data = new TerrainData
            {
                heightmapResolution = res,
                size = new Vector3(Size, 42f, Size),
            };
            var h = new float[res, res];
            float ox = seed * 0.37f, oz = seed * 0.19f;
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float wx = x / (float)(res - 1) * Size - Size * 0.5f;
                    float wz = y / (float)(res - 1) * Size - Size * 0.5f;
                    float n = Mathf.PerlinNoise((wx + ox) * 0.004f, (wz + oz) * 0.004f) * 0.7f
                            + Mathf.PerlinNoise((wx + ox) * 0.012f, (wz + oz) * 0.012f) * 0.25f;
                    float flat = Mathf.Clamp01(Mathf.Sqrt(wx * wx + wz * wz) / 140f);
                    h[y, x] = Mathf.Lerp(0.02f, n, flat) * 0.5f + 0.5f * 0.02f;
                }
            }
            data.SetHeights(0, 0, h);

            var layer = new TerrainLayer
            {
                diffuseTexture = SolidTexture(new Color(0.36f, 0.38f, 0.22f)),
                tileSize = new Vector2(20f, 20f),
                name = "ground",
            };
            data.terrainLayers = new[] { layer };

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-Size * 0.5f, 0f, -Size * 0.5f);
            var t = go.GetComponent<Terrain>();
            t.drawInstanced = true;
            return t;
        }

        static Texture2D SolidTexture(Color c)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = c * (0.92f + 0.08f * Random.value);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static void CreateHouse(Transform parent, Vector3 pos, float yaw)
        {
            var house = new GameObject("House");
            house.transform.SetParent(parent, false);
            house.transform.position = pos;
            house.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(house.transform, false);
            body.transform.localPosition = new Vector3(0f, 3f, 0f);
            body.transform.localScale = new Vector3(8f, 6f, 6f);
            body.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Lit(new Color(0.7f, 0.6f, 0.48f), 0f, 0.1f);

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(house.transform, false);
            roof.transform.localPosition = new Vector3(0f, 6.6f, 0f);
            roof.transform.localScale = new Vector3(8.6f, 1.2f, 6.6f);
            roof.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Lit(new Color(0.35f, 0.18f, 0.14f), 0f, 0.1f);
            Object.Destroy(roof.GetComponent<Collider>());

            var d = house.AddComponent<Destructible>();
            d.MaxHp = 900f;
            d.Hp = 900f;
            d.Crushable = true;
            d.DebrisCount = 14;
            d.DebrisColor = new Color(0.7f, 0.6f, 0.48f);
            d.DisplayName = "House";
            // Single compound collider lives on the root so tanks and shells reach Destructible.
            var box = house.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 3f, 0f);
            box.size = new Vector3(8f, 6f, 6f);
            Object.Destroy(body.GetComponent<Collider>());
            house.AddComponent<Rigidbody>().isKinematic = true;
        }

        static void CreateWall(Transform parent, Vector3 pos, float length)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos + Vector3.up * 1.2f;
            wall.transform.localScale = new Vector3(length, 2.4f, 0.8f);
            wall.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Lit(new Color(0.55f, 0.53f, 0.5f), 0f, 0.1f);
            var d = wall.AddComponent<Destructible>();
            d.MaxHp = 700f; d.Hp = 700f; d.Crushable = true; d.CrushImpulse = 4f; d.DebrisCount = 8;
            d.DebrisColor = new Color(0.55f, 0.53f, 0.5f); d.DisplayName = "Wall";
            wall.AddComponent<Rigidbody>().isKinematic = true;
        }

        static void CreateTree(Transform parent, Vector3 pos, float scale)
        {
            var tree = new GameObject("Tree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;
            tree.transform.localScale = Vector3.one * scale;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            trunk.transform.localScale = new Vector3(0.45f, 2.5f, 0.45f);
            trunk.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Lit(new Color(0.3f, 0.22f, 0.15f), 0f, 0.05f);

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(tree.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 6.2f, 0f);
            canopy.transform.localScale = new Vector3(3.6f, 3.2f, 3.6f);
            canopy.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Lit(new Color(0.2f, 0.32f, 0.16f), 0f, 0.05f);
            Object.Destroy(canopy.GetComponent<Collider>());

            // Keep the trunk collider on the root so OnCollisionEnter and the topple code work.
            Object.Destroy(trunk.GetComponent<Collider>());
            var cap = tree.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 2.5f, 0f);
            cap.height = 5f;
            cap.radius = 0.5f;
            tree.AddComponent<TreeProp>();
        }
    }
}

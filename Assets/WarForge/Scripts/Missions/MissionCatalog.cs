using System.Collections.Generic;

namespace WarForge.Missions
{
    public struct EnemySpawn
    {
        public string VehicleId;
        public float X, Z;      // arena-relative position
    }

    public class MissionDefinition
    {
        public string Id;
        public string Title;
        public string Briefing;
        public string UnlockAfter;   // mission id that must be completed first, empty = none
        public float TimeLimitSeconds; // 0 = no limit
        public int Seed;
        public float ArenaRadius = 320f;
        public EnemySpawn[] Enemies;
        public (float x, float z)[] PatrolLoop;   // enemy patrol loop (arena-relative)
        public string ArenaTheme = "farmland";   // placeholder for later biome packs
    }

    /// <summary>Offline mission list. Add new missions here; data-only changes need no gameplay code.</summary>
    public static class MissionCatalog
    {
        static List<MissionDefinition> all;

        public static IReadOnlyList<MissionDefinition> All
        {
            get
            {
                if (all == null) Build();
                return all;
            }
        }

        public static MissionDefinition Get(string id)
        {
            foreach (var m in All) if (m.Id == id) return m;
            return All[0];
        }

        static void Build()
        {
            var loopA = new[] { (120f, 120f), (-120f, 110f), (-100f, -120f), (110f, -110f) };
            var loopB = new[] { (60f, 60f), (-80f, 40f), (-40f, -90f), (90f, -60f) };

            all = new List<MissionDefinition>
            {
                new MissionDefinition
                {
                    Id = "training_range", Title = "Training Range",
                    Briefing = "Three hostile scouts and a medium are patrolling the range. Destroy them all. Learn the shell types, the gunner sight and the track damage.",
                    UnlockAfter = "", TimeLimitSeconds = 0f, Seed = 101,
                    Enemies = new[]
                    {
                        new EnemySpawn { VehicleId = "en_skirmisher", X = 140f, Z = 90f },
                        new EnemySpawn { VehicleId = "en_skirmisher", X = -150f, Z = 80f },
                        new EnemySpawn { VehicleId = "en_kestrel", X = 0f, Z = 190f },
                    },
                    PatrolLoop = loopA,
                },
                new MissionDefinition
                {
                    Id = "ridge_hold", Title = "Ridge Hold",
                    Briefing = "Hold the ridge for 10 minutes against a mixed platoon, including a Goliath. Do not trade head-on with the heavy: flank it.",
                    UnlockAfter = "training_range", TimeLimitSeconds = 600f, Seed = 202,
                    Enemies = new[]
                    {
                        new EnemySpawn { VehicleId = "en_skirmisher", X = 160f, Z = 140f },
                        new EnemySpawn { VehicleId = "en_kestrel", X = -120f, Z = 150f },
                        new EnemySpawn { VehicleId = "en_kestrel", X = 40f, Z = 220f },
                        new EnemySpawn { VehicleId = "en_goliath", X = -190f, Z = 40f },
                        new EnemySpawn { VehicleId = "en_skirmisher", X = 190f, Z = -40f },
                    },
                    PatrolLoop = loopB,
                },
                new MissionDefinition
                {
                    Id = "ironvale_assault", Title = "Ironvale Assault",
                    Briefing = "Break through the Ironvale line. Unlocks the WF-6 Bastion heavy. Clear every enemy to win.",
                    UnlockAfter = "ridge_hold", TimeLimitSeconds = 900f, Seed = 303,
                    Enemies = new[]
                    {
                        new EnemySpawn { VehicleId = "en_goliath", X = 0f, Z = 210f },
                        new EnemySpawn { VehicleId = "en_kestrel", X = -170f, Z = 120f },
                        new EnemySpawn { VehicleId = "en_kestrel", X = 170f, Z = 110f },
                        new EnemySpawn { VehicleId = "en_skirmisher", X = -60f, Z = 260f },
                    },
                    PatrolLoop = loopA,
                },
            };
        }
    }
}

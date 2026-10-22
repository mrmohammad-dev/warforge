using NUnit.Framework;
using WarForge.Core;
using WarForge.Data;
using WarForge.Missions;

namespace WarForge.Tests
{
    public class DataTests
    {
        [Test]
        public void EveryVehicleHasShellsAndUniqueId()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var v in VehicleCatalog.All)
            {
                Assert.That(seen.Add(v.id), Is.True, "duplicate id " + v.id);
                Assert.That(v.shells, Is.Not.Null.And.Not.Empty, v.id);
                Assert.That(v.massKg, Is.GreaterThan(1000f), v.id);
            }
        }

        [Test]
        public void PlayerVehiclesAreSelectableAndUnlockReferencesExist()
        {
            foreach (var v in VehicleCatalog.PlayerVehicles())
            {
                Assert.That(v.playerSelectable, Is.True);
                if (!string.IsNullOrEmpty(v.unlockedByMission))
                    Assert.That(MissionCatalog.All, Has.Some.Matches<MissionDefinition>(m => m.Id == v.unlockedByMission));
            }
        }

        [Test]
        public void MissionUnlockChainIsValid()
        {
            foreach (var m in MissionCatalog.All)
            {
                if (!string.IsNullOrEmpty(m.UnlockAfter))
                    Assert.That(MissionCatalog.All, Has.Some.Matches<MissionDefinition>(x => x.Id == m.UnlockAfter));
                Assert.That(m.Enemies, Is.Not.Empty, m.Id);
                foreach (var e in m.Enemies) Assert.That(VehicleCatalog.Exists(e.VehicleId), e.VehicleId);
            }
        }

        [Test]
        public void ShellPenetrationDropsWithDistance()
        {
            var s = new ShellSpec { penetration = 100f, penetrationLossPer1000m = 0.2f };
            Assert.That(s.PenetrationAt(0f), Is.EqualTo(100f));
            Assert.That(s.PenetrationAt(1000f), Is.EqualTo(80f).Within(0.01f));
        }

        [Test]
        public void SaveJsonRoundTrips()
        {
            var d = new SaveData { credits = 250, selectedVehicleId = "wf_lynx" };
            d.GetOrCreate("training_range").completed = true;
            var back = SaveSystem.FromJson(SaveSystem.ToJson(d));
            Assert.That(back.credits, Is.EqualTo(250));
            Assert.That(back.selectedVehicleId, Is.EqualTo("wf_lynx"));
            Assert.That(back.IsCompleted("training_range"), Is.True);
        }

        [Test]
        public void CorruptOrEmptySaveFallsBackSafely()
        {
            Assert.That(SaveSystem.FromJson(""), Is.Null);
            var s = new SaveData { credits = -5, selectedVehicleId = "" };
            s.Sanitize();
            Assert.That(s.credits, Is.EqualTo(0));
            Assert.That(s.selectedVehicleId, Is.EqualTo("wf_warden"));
        }
    }
}

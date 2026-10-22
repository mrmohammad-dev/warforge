using System.Collections.Generic;
using UnityEngine;
using WarForge.Combat;
using WarForge.Data;
using WarForge.FX;

namespace WarForge.Vehicles
{
    /// <summary>
    /// Builds a complete vehicle from a VehicleSpec at runtime: visual hull/turret/gun from primitives (placeholder for
    /// authored meshes), armor colliders with ArmorPlate data, damage modules, controllers and audio.
    /// Swap the Build* helpers for prefab/mesh loading when real models exist.
    /// </summary>
    public static class VehicleFactory
    {
        public static Vehicle Create(VehicleSpec spec, Team team, Vector3 position, Quaternion rotation)
        {
            var root = new GameObject(spec.id);
            root.transform.SetPositionAndRotation(position, rotation);
            root.layer = 0;
            var vehicle = root.AddComponent<Vehicle>();
            vehicle.Spec = spec;
            vehicle.Team = team;
            vehicle.Renderers = new Renderer[0];

            var body = root.AddComponent<Rigidbody>();
            vehicle.Body = body;

            var hullSize = spec.hullSize;
            Color hullColor = spec.camoA;
            Color accent = spec.camoB;
            Color dark = spec.camoC;
            var renderers = new List<Renderer>();

            // ---- Hull (armor box + sloped glacis) ----
            var hullBox = Box(root.transform, "Hull", Vector3.zero, hullSize, hullColor, 0.05f, renderers);
            var hullPlate = hullBox.AddComponent<ArmorPlate>();
            hullPlate.Vehicle = vehicle;
            hullPlate.PlateName = "Hull";
            hullPlate.SetFaces(spec.armor.hullFront, spec.armor.hullSide, spec.armor.hullRear, spec.armor.hullTop, spec.armor.hullBottom);

            // Glacis: thin sloped plate covering the front upper hull.
            float slope = spec.armor.upperFrontSlopeDeg;
            var glacis = Box(root.transform, "Glacis", new Vector3(0f, hullSize.y * 0.2f, hullSize.z * 0.5f - 0.2f),
                new Vector3(hullSize.x * 0.92f, 0.12f, 1.2f), hullColor * 1.05f, 0.05f, renderers);
            glacis.transform.localRotation = Quaternion.Euler(-(90f - slope), 0f, 0f);
            var glacisPlate = glacis.AddComponent<ArmorPlate>();
            glacisPlate.Vehicle = vehicle; glacisPlate.PlateName = "Glacis"; glacisPlate.SetUniform(spec.armor.upperFront);

            // Tracks (spaced armor, also driven by damage).
            float trackW = 0.5f;
            float trackX = hullSize.x * 0.5f - trackW * 0.5f;
            var tl = Box(root.transform, "TrackLeft", new Vector3(-trackX, -0.1f, 0f), new Vector3(trackW, hullSize.y * 0.8f, hullSize.z), dark, 0.2f, renderers);
            var tr = Box(root.transform, "TrackRight", new Vector3(trackX, -0.1f, 0f), new Vector3(trackW, hullSize.y * 0.8f, hullSize.z), dark, 0.2f, renderers);
            foreach (var pair in new[] { (tl, ModuleType.TrackLeft), (tr, ModuleType.TrackRight) })
            {
                var p = pair.Item1.AddComponent<ArmorPlate>();
                p.Vehicle = vehicle; p.PlateName = "Track"; p.IsTrack = true; p.TrackModule = pair.Item2; p.SetUniform(spec.armor.track);
            }
            AddRoadWheels(root.transform, hullSize, spec.roadWheelsPerSide, dark, renderers);

            // ---- Turret ----
            var turretPivot = new GameObject("Turret").transform;
            turretPivot.SetParent(root.transform, false);
            turretPivot.localPosition = new Vector3(0f, hullSize.y * 0.5f + spec.turretSize.y * 0.5f - 0.05f, spec.turretOffsetZ);
            var turretBox = Box(turretPivot, "TurretBody", Vector3.zero, spec.turretSize, hullColor, 0.05f, renderers);
            var turretPlate = turretBox.AddComponent<ArmorPlate>();
            turretPlate.Vehicle = vehicle; turretPlate.PlateName = "Turret";
            turretPlate.SetFaces(spec.armor.turretFront, spec.armor.turretSide, spec.armor.turretRear, spec.armor.turretTop, spec.armor.turretTop);
            var cupola = Box(turretPivot, "Cupola", new Vector3(-spec.turretSize.x * 0.2f, spec.turretSize.y * 0.6f, -0.2f), new Vector3(0.5f, 0.35f, 0.5f), accent, 0.05f, renderers);
            Object.Destroy(cupola.GetComponent<Collider>()); // decorative; crew modules handle damage

            var gunPivot = new GameObject("GunPivot").transform;
            gunPivot.SetParent(turretPivot, false);
            gunPivot.localPosition = new Vector3(0f, 0f, spec.turretSize.z * 0.5f - 0.1f);

            var mantlet = Box(gunPivot, "Mantlet", Vector3.zero, new Vector3(spec.turretSize.x * 0.5f, spec.turretSize.y * 0.5f, 0.35f), hullColor * 0.95f, 0.1f, renderers);
            var mantletPlate = mantlet.AddComponent<ArmorPlate>();
            mantletPlate.Vehicle = vehicle; mantletPlate.PlateName = "Mantlet"; mantletPlate.SetUniform(spec.armor.mantlet);

            var barrel = Cylinder(gunPivot, "Barrel", new Vector3(0f, 0f, spec.barrelLength * 0.5f + 0.2f), spec.barrelRadius, spec.barrelLength, dark, renderers);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(gunPivot, false);
            muzzle.localPosition = new Vector3(0f, 0f, spec.barrelLength + 0.2f);

            var sight = new GameObject("SightPoint").transform;
            sight.SetParent(turretPivot, false);
            sight.localPosition = new Vector3(0.4f, spec.turretSize.y * 0.6f, spec.turretSize.z * 0.3f);

            // ---- Visual detailing (cheap): exhaust, stowage bins, headlight ----
            Object.Destroy(Box(root.transform, "Exhaust", new Vector3(hullSize.x * 0.3f, hullSize.y * 0.6f, -hullSize.z * 0.5f), new Vector3(0.25f, 0.25f, 0.5f), dark, 0.2f, renderers).GetComponent<Collider>());
            Object.Destroy(Box(turretPivot, "Stowage", new Vector3(0f, spec.turretSize.y * 0.6f, -spec.turretSize.z * 0.5f), new Vector3(spec.turretSize.x * 0.8f, 0.25f, 0.3f), dark, 0.1f, renderers).GetComponent<Collider>());

            vehicle.Renderers = renderers.ToArray();
            vehicle.TurretTransform = turretPivot;
            vehicle.GunPivot = gunPivot;
            vehicle.Muzzle = muzzle;
            vehicle.SightPoint = sight;

            // ---- Controllers ----
            body.mass = spec.massKg;
            var ctrl = root.AddComponent<TankController>();
            ctrl.Init(spec, body, hullSize);
            vehicle.Controller = ctrl;

            var turret = root.AddComponent<TurretController>();
            turret.Turret = turretPivot;
            turret.Gun = gunPivot;
            turret.TraverseDegPerSec = spec.turretTraverseDeg;
            turret.ElevationDegPerSec = spec.gunElevationSpeedDeg;
            turret.MinElevation = spec.minElevationDeg;
            turret.MaxElevation = spec.maxElevationDeg;
            vehicle.Turret = turret;

            var gun = root.AddComponent<TankGun>();
            gun.Init(vehicle, spec.shells, spec.reloadSeconds);
            vehicle.Gun = gun;

            // ---- Damage model ----
            var dmg = root.AddComponent<DamageModel>();
            dmg.Init(vehicle, spec.structureHp);
            float hz = hullSize.z * 0.5f;
            dmg.AddModule(ModuleType.Driver, root.transform, new Vector3(-0.5f, 0f, hz * 0.5f), new Vector3(0.7f, hullSize.y, 1.4f), 100f);
            dmg.AddModule(ModuleType.Engine, root.transform, new Vector3(0f, 0f, -hz * 0.6f), new Vector3(hullSize.x * 0.8f, hullSize.y, 1.6f), 220f);
            dmg.AddModule(ModuleType.TrackLeft, root.transform, new Vector3(-trackX, 0f, 0f), new Vector3(trackW, hullSize.y, hullSize.z), 180f);
            dmg.AddModule(ModuleType.TrackRight, root.transform, new Vector3(trackX, 0f, 0f), new Vector3(trackW, hullSize.y, hullSize.z), 180f);
            dmg.AddModule(ModuleType.AmmoRack, root.transform, new Vector3(0.5f, -0.2f, -0.2f), new Vector3(0.9f, hullSize.y * 0.6f, hullSize.z * 0.5f), 140f);
            dmg.AddModule(ModuleType.Gunner, turretPivot, new Vector3(-spec.turretSize.x * 0.2f, 0f, 0.1f), spec.turretSize * 0.7f, 90f);
            dmg.AddModule(ModuleType.Loader, turretPivot, new Vector3(spec.turretSize.x * 0.2f, 0f, -0.2f), spec.turretSize * 0.7f, 90f);
            dmg.AddModule(ModuleType.Commander, turretPivot, new Vector3(-spec.turretSize.x * 0.2f, spec.turretSize.y * 0.4f, -0.2f), spec.turretSize * 0.5f, 90f);
            dmg.AddModule(ModuleType.Breech, gunPivot, new Vector3(0f, 0f, -0.4f), new Vector3(0.6f, 0.6f, 1.0f), 120f);
            dmg.AddModule(ModuleType.TurretDrive, turretPivot, new Vector3(0f, -spec.turretSize.y * 0.5f, 0f), new Vector3(spec.turretSize.x * 0.6f, 0.4f, spec.turretSize.z * 0.6f), 120f);
            vehicle.Damage = dmg;

            // ---- Audio ----
            root.AddComponent<AudioSource>();
            var audio = root.AddComponent<TankAudio>();
            audio.Init(ctrl);
            vehicle.Audio = audio;

            return vehicle;
        }

        static void AddRoadWheels(Transform parent, Vector3 hullSize, int count, Color color, List<Renderer> rs)
        {
            count = Mathf.Clamp(count, 3, 8);
            float z0 = -hullSize.z * 0.4f, z1 = hullSize.z * 0.4f;
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < count; i++)
                {
                    float z = Mathf.Lerp(z0, z1, count == 1 ? 0.5f : i / (float)(count - 1));
                    var w = Cylinder(parent, "RoadWheel", new Vector3(s * (hullSize.x * 0.5f + 0.02f), -hullSize.y * 0.35f, z), 0.22f, 0.12f, color, rs);
                    w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }
        }

        static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Color color, float metallic, List<Renderer> rs)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = MaterialLibrary.Lit(color, metallic, 0.2f);
            if (rs != null) rs.Add(r);
            return go;
        }

        static GameObject Cylinder(Transform parent, string name, Vector3 localPos, float radius, float length, Color color, List<Renderer> rs)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = MaterialLibrary.Lit(color, 0.4f, 0.4f);
            if (rs != null) rs.Add(r);
            // Barrels and wheels are visual only: they must not block shells or suspension rays.
            Object.Destroy(go.GetComponent<Collider>());
            return go;
        }
    }
}

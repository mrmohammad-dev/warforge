using System.Collections.Generic;
using UnityEngine;
using WarForge.AI;
using WarForge.Cameras;
using WarForge.Combat;
using WarForge.Controls;
using WarForge.Data;
using WarForge.Missions;
using WarForge.Vehicles;
using WarForge.World;

namespace WarForge.Core
{
    public enum GameScreen { Main, Garage, Missions, Settings, Playing, Paused, Result }

    /// <summary>
    /// Top-level flow: menus, mission lifecycle, pause, restart and results. Creates the camera, input and arena.
    /// Restart reloads the Main scene (fully clean state) and asks the bootstrap to start the same mission again.
    /// UI drawing lives in GameRoot.UI.cs (partial).
    /// </summary>
    public partial class GameRoot : MonoBehaviour
    {
        public GameScreen Page { get; private set; } = GameScreen.Main;
        public Vehicle Player { get; private set; }
        public MissionDefinition Mission { get; private set; }
        public MissionTracker Tracker { get; private set; }
        public VehicleCameraRig Rig { get; private set; }
        public Camera Cam { get; private set; }
        public TouchControls Touch { get; private set; }
        public string Notice = "";
        public SaveData Save => SaveSystem.Data;

        readonly List<EnemyTankAI> enemyAI = new List<EnemyTankAI>();
        bool arenaBuilt;
        float noticeTimer;
        float resultDelay;

        void Awake()
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            Cam = camGo.AddComponent<Camera>();
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.55f, 0.66f, 0.8f);
            Cam.nearClipPlane = 0.2f;
            Cam.farClipPlane = 1200f;
            Cam.fieldOfView = 60f;
            camGo.AddComponent<AudioListener>();
            Rig = camGo.AddComponent<VehicleCameraRig>();
            Rig.Cam = Cam;

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Touch = gameObject.AddComponent<TouchControls>();
            Application.runInBackground = false;
        }

        void Start()
        {
            SaveSystem.Load();
            SettingsApplier.Apply(Save.settings);
            Rig.Sensitivity = Save.settings.lookSensitivity;
            Rig.InvertY = Save.settings.invertY;
            if (GameSession.StartMissionImmediately)
            {
                GameSession.StartMissionImmediately = false;
                StartMission(GameSession.SelectedMissionId);
            }
            else
            {
                SetScreen(GameScreen.Main);
            }
        }

        public void SetScreen(GameScreen s)
        {
            Page = s;
            if (s != GameScreen.Playing && s != GameScreen.Paused && s != GameScreen.Result)
                Time.timeScale = 1f;
        }

        public bool IsUnlocked(VehicleSpec v)
        {
            if (string.IsNullOrEmpty(v.unlockedByMission)) return true;
            return Save.IsCompleted(v.unlockedByMission);
        }

        public bool IsUnlocked(MissionDefinition m)
        {
            if (string.IsNullOrEmpty(m.UnlockAfter)) return true;
            return Save.IsCompleted(m.UnlockAfter);
        }

        public void SelectVehicle(VehicleSpec v)
        {
            if (!IsUnlocked(v)) return;
            Save.selectedVehicleId = v.id;
            SaveSystem.Save();
        }

        public void StartMission(string missionId)
        {
            var m = MissionCatalog.Get(missionId);
            if (!IsUnlocked(m)) { Notice = "Mission is locked."; return; }
            if (arenaBuilt)
            {
                // A world already exists in this scene: reload for a clean slate instead of stacking arenas.
                GameSession.SelectedMissionId = m.Id;
                GameSession.StartMissionImmediately = true;
                Tracker?.End();
                UnityEngine.SceneManagement.SceneManager.LoadScene(GameBootstrap.MainSceneName);
                return;
            }
            arenaBuilt = true;
            Mission = m;
            GameSession.SelectedMissionId = m.Id;

            var arena = ArenaBuilder.Build(m, transform);

            var spec = VehicleCatalog.Get(Save.selectedVehicleId);
            if (!spec.playerSelectable || !IsUnlocked(spec)) spec = VehicleCatalog.Get("wf_warden");
            Player = VehicleFactory.Create(spec, Team.Player, arena.PlayerSpawn, Quaternion.LookRotation(arena.PlayerSpawnForward));

            var enemies = new List<Vehicle>();
            enemyAI.Clear();
            for (int i = 0; i < arena.EnemySpawns.Count; i++)
            {
                var es = VehicleCatalog.Get(m.Enemies[i].VehicleId);
                var ev = VehicleFactory.Create(es, Team.Enemy, arena.EnemySpawns[i], Quaternion.LookRotation(arena.EnemyForwards[i]));
                var ai = ev.gameObject.AddComponent<EnemyTankAI>();
                ai.Waypoints = arena.PatrolPoints;
                ai.Accuracy = 0.7f + 0.05f * (i % 4);
                enemyAI.Add(ai);
                enemies.Add(ev);
            }

            Rig.SetTarget(Player);
            Rig.Mode = CameraMode.ThirdPerson;
            Rig.NormalFov = 60f;
            Tracker = new MissionTracker(m);
            Tracker.Begin(Player, enemies);
            Notice = "";
            noticeTimer = 0f;
            SetScreen(GameScreen.Playing);
        }

        public void TogglePause()
        {
            if (Page == GameScreen.Playing) { Page = GameScreen.Paused; Time.timeScale = 0f; }
            else if (Page == GameScreen.Paused) { Page = GameScreen.Playing; Time.timeScale = 1f; }
        }

        public void RestartMission()
        {
            Time.timeScale = 1f;
            GameSession.SelectedMissionId = Mission != null ? Mission.Id : GameSession.SelectedMissionId;
            GameSession.StartMissionImmediately = true;
            Tracker?.End();
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameBootstrap.MainSceneName);
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            Tracker?.End();
            GameSession.StartMissionImmediately = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameBootstrap.MainSceneName);
        }

        void OnDestroy()
        {
            Tracker?.End();
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (noticeTimer > 0f) noticeTimer -= Time.unscaledDeltaTime;

            if (Page == GameScreen.Playing || Page == GameScreen.Paused)
            {
                var i = Touch.Intent;
                if (i.Pause) TogglePause();
                if (Page == GameScreen.Playing) UpdatePlaying(i);
            }
            else if (Page == GameScreen.Result)
            {
                resultDelay += Time.unscaledDeltaTime;
            }
        }

        void UpdatePlaying(Controls.DriveIntent i)
        {
            if (Player == null || Tracker == null) return;

            Tracker.Tick(Time.deltaTime);
            if (Tracker.State != MissionState.Running)
            {
                FinishMission();
                return;
            }

            if (Player.Damage.IsDestroyed) return;

            var ctrl = Player.Controller;
            ctrl.Throttle = i.Throttle;
            ctrl.Steer = i.Steer;
            if (i.ToggleView) Rig.ToggleMode();
            if (i.CycleShell) Player.Gun.CycleShell();
            if (i.Fire)
            {
                if (!Player.Gun.TryFire() && Player.Gun.BreechDisabled) Notice = "Breech damaged.";
            }
            Rig.Look(i.AimDelta);
        }

        void FinishMission()
        {
            if (Page == GameScreen.Result) return;
            Page = GameScreen.Result;
            resultDelay = 0f;
            Player.Controller.Throttle = 0f;
            Player.Controller.Steer = 0f;
            Player.Turret.ClearAim();

            if (Tracker.State == MissionState.Won)
            {
                var rec = Save.GetOrCreate(Mission.Id);
                rec.completed = true;
                if (rec.bestTime <= 0f || Tracker.Elapsed < rec.bestTime) rec.bestTime = Tracker.Elapsed;
                rec.bestKills = Mathf.Max(rec.bestKills, Tracker.Kills);
                Save.credits += 100 + Tracker.Kills * 50;
            }
            else
            {
                var rec = Save.GetOrCreate(Mission.Id);
                rec.bestKills = Mathf.Max(rec.bestKills, Tracker.Kills);
            }
            SaveSystem.Save();
        }

        public void ApplySettings()
        {
            Save.settings.Sanitize();
            SettingsApplier.Apply(Save.settings);
            Rig.Sensitivity = Save.settings.lookSensitivity;
            Rig.InvertY = Save.settings.invertY;
            SaveSystem.Save();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && Page == GameScreen.Playing) TogglePause();
            if (paused) SaveSystem.Save();
        }

        void OnApplicationQuit() => SaveSystem.Save();
    }
}

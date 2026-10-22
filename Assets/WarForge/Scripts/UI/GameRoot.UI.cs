using System.Collections.Generic;
using UnityEngine;
using WarForge.Combat;
using WarForge.Data;
using WarForge.Missions;

namespace WarForge.Core
{
    /// <summary>
    /// IMGUI screens (menus, HUD, pause, results). IMGUI keeps the vertical slice dependency-free and easy to
    /// audit; it can be replaced by uGUI prefabs later without touching gameplay code.
    /// All drawing uses a 720-pixel-high virtual canvas so text and buttons scale across phones.
    /// </summary>
    public partial class GameRoot
    {
        const float VirtualH = 720f;
        readonly Queue<string> hitLog = new Queue<string>();
        GUIStyle titleStyle, bodyStyle, buttonStyle, smallStyle;
        Texture2D panelTex, barBgTex, barFgTex;
        float uiScale = 1f;
        GameScreen settingsReturn = GameScreen.Main;

        void OnEnable() => CombatEvents.HitReported += OnHitReported;
        void OnDisable() => CombatEvents.HitReported -= OnHitReported;

        void OnHitReported(HitReport r)
        {
            if (Player == null) return;
            if (r.Victim != Player && r.Attacker != Player) return;
            string who = r.Victim == Player ? "You" : (r.Victim != null ? r.Victim.DisplayName : "Target");
            string text;
            if (r.Victim == Player && r.Outcome == HitOutcome.Ricochet) text = "Ricochet on " + r.PlateName;
            else if (r.Victim == Player && r.Outcome == HitOutcome.NonPenetration) text = "Bounced off " + r.PlateName + " (" + Mathf.RoundToInt(r.EffectiveArmor) + "mm eff)";
            else if (r.Outcome == HitOutcome.Penetration) text = (r.Attacker == Player ? "Penetrated " : "Penetrated by ") + who + " " + r.PlateName + ": " + (string.IsNullOrEmpty(r.ModulesSummary) ? "no internal damage" : r.ModulesSummary);
            else text = "Hit on " + who;
            hitLog.Enqueue(text);
            while (hitLog.Count > 4) hitLog.Dequeue();
        }

        void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(0.92f, 0.88f, 0.7f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, alignment = TextAnchor.UpperLeft };
            bodyStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, alignment = TextAnchor.UpperLeft };
            smallStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold, padding = new RectOffset(8, 8, 10, 10) };
            panelTex = Tex(new Color(0.05f, 0.06f, 0.05f, 0.82f));
            barBgTex = Tex(new Color(0f, 0f, 0f, 0.6f));
            barFgTex = Tex(Color.white);
        }

        static Texture2D Tex(Color c)
        {
            var t = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
            var px = new Color[4] { c, c, c, c };
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        void OnGUI()
        {
            EnsureStyles();
            uiScale = Mathf.Max(1f, UnityEngine.Screen.height / VirtualH);
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
            float W = UnityEngine.Screen.width / uiScale, H = UnityEngine.Screen.height / uiScale;

            if (Page == GameScreen.Main || Page == GameScreen.Garage || Page == GameScreen.Missions || Page == GameScreen.Settings)
            {
                DrawBackdrop(W, H);
            }

            switch (Page)
            {
                case GameScreen.Main: DrawMain(W, H); break;
                case GameScreen.Garage: DrawGarage(W, H); break;
                case GameScreen.Missions: DrawMissions(W, H); break;
                case GameScreen.Settings: DrawSettings(W, H); break;
                case GameScreen.Playing: DrawHud(W, H); break;
                case GameScreen.Paused: DrawHud(W, H); DrawPause(W, H); break;
                case GameScreen.Result: DrawHud(W, H); DrawResult(W, H); break;
            }
        }

        void DrawBackdrop(float W, float H)
        {
            GUI.color = new Color(0.12f, 0.14f, 0.11f, 1f);
            GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void Panel(Rect r) => GUI.DrawTexture(r, panelTex);

        bool Btn(Rect r, string text) => GUI.Button(r, text, buttonStyle);

        void DrawMain(float W, float H)
        {
            float cx = W * 0.5f;
            GUI.Label(new Rect(0, H * 0.12f, W, 60), "WARFORGE", titleStyle);
            GUI.Label(new Rect(0, H * 0.12f + 52, W, 30), "armored combat, offline", smallStyle);
            float bw = 300, bh = 52, y = H * 0.36f;
            if (Btn(new Rect(cx - bw / 2, y, bw, bh), "Missions")) SetScreen(GameScreen.Missions);
            if (Btn(new Rect(cx - bw / 2, y + 66, bw, bh), "Garage")) SetScreen(GameScreen.Garage);
            if (Btn(new Rect(cx - bw / 2, y + 132, bw, bh), "Settings")) { settingsReturn = GameScreen.Main; Page = GameScreen.Settings; }
            if (Btn(new Rect(cx - bw / 2, y + 198, bw, bh), "Quit")) Application.Quit();
            if (!string.IsNullOrEmpty(Notice)) GUI.Label(new Rect(0, H - 40, W, 30), Notice, smallStyle);
        }

        void DrawGarage(float W, float H)
        {
            GUI.Label(new Rect(0, 20, W, 50), "GARAGE", titleStyle);
            if (Btn(new Rect(20, 20, 120, 44), "Back")) SetScreen(GameScreen.Main);
            var list = VehicleCatalog.PlayerVehicles();
            float y = 100;
            foreach (var v in list)
            {
                bool unlocked = IsUnlocked(v);
                bool selected = Save.selectedVehicleId == v.id;
                Panel(new Rect(40, y, W - 80, 96));
                string label = (selected ? "[SELECTED] " : "") + v.displayName + " (" + v.vehicleClass + ")";
                GUI.Label(new Rect(56, y + 6, W - 300, 30), label, bodyStyle);
                string info = v.description + (unlocked ? "" : "  LOCKED: complete '" + v.unlockedByMission + "'") +
                              "\nSpeed " + v.maxForwardKph + " km/h · Gun " + (v.shells.Length > 0 ? v.shells[0].caliberMm + " mm" : "-") +
                              " · Armor front " + v.armor.hullFront + " / turret " + v.armor.turretFront;
                GUI.Label(new Rect(56, y + 34, W - 300, 60), info, smallStyle);
                if (unlocked && !selected && Btn(new Rect(W - 220, y + 26, 160, 44), "Select")) SelectVehicle(v);
                y += 108;
            }
            if (Save.credits > 0) GUI.Label(new Rect(40, H - 34, W, 30), "Credits: " + Save.credits, smallStyle);
        }

        void DrawMissions(float W, float H)
        {
            GUI.Label(new Rect(0, 20, W, 50), "MISSIONS", titleStyle);
            if (Btn(new Rect(20, 20, 120, 44), "Back")) SetScreen(GameScreen.Main);
            float y = 100;
            foreach (var m in MissionCatalog.All)
            {
                bool unlocked = IsUnlocked(m);
                var rec = Save.Find(m.Id);
                Panel(new Rect(40, y, W - 80, 110));
                string status = rec != null && rec.completed ? "  [COMPLETE]" : (unlocked ? "" : "  [LOCKED]");
                GUI.Label(new Rect(56, y + 6, W - 300, 30), m.Title + status, bodyStyle);
                string time = m.TimeLimitSeconds > 0 ? " · limit " + (int)(m.TimeLimitSeconds / 60) + " min" : "";
                GUI.Label(new Rect(56, y + 36, W - 300, 70), m.Briefing + time, smallStyle);
                if (unlocked && Btn(new Rect(W - 220, y + 32, 160, 44), "Deploy")) StartMission(m.Id);
                y += 122;
            }
        }

        void DrawSettings(float W, float H)
        {
            var s = Save.settings;
            GUI.Label(new Rect(0, 20, W, 50), "SETTINGS", titleStyle);
            if (Btn(new Rect(20, 20, 120, 44), "Back"))
            {
                ApplySettings();
                if (settingsReturn == GameScreen.Paused) Page = GameScreen.Paused;
                else SetScreen(GameScreen.Main);
            }
            float x = W * 0.25f, y = 110, w = W * 0.5f;

            GUI.Label(new Rect(x, y, w, 30), "Look sensitivity: " + s.lookSensitivity.ToString("0.00"), bodyStyle);
            s.lookSensitivity = GUI.HorizontalSlider(new Rect(x, y + 34, w, 24), s.lookSensitivity, 0.2f, 3f);
            y += 80;
            GUI.Label(new Rect(x, y, w, 30), "Master volume: " + Mathf.RoundToInt(s.masterVolume * 100) + "%", bodyStyle);
            s.masterVolume = GUI.HorizontalSlider(new Rect(x, y + 34, w, 24), s.masterVolume, 0f, 1f);
            y += 80;
            if (Btn(new Rect(x, y, w, 46), "Invert vertical look: " + (s.invertY ? "ON" : "OFF"))) s.invertY = !s.invertY;
            y += 60;
            if (Btn(new Rect(x, y, w, 46), "Graphics: " + SettingsApplier.QualityNames[s.quality])) s.quality = (s.quality + 1) % 3;
            y += 60;
            if (Btn(new Rect(x, y, w, 46), "Ballistic assist: " + (s.ballisticAssist ? "ON" : "OFF"))) s.ballisticAssist = !s.ballisticAssist;
            y += 60;
            if (Btn(new Rect(x, y, w, 46), "Show FPS: " + (s.showFps ? "ON" : "OFF"))) s.showFps = !s.showFps;
            if (s.showFps) GUI.Label(new Rect(x, y + 56, w, 30), "FPS " + Mathf.RoundToInt(1f / Mathf.Max(0.001f, Time.unscaledDeltaTime)), smallStyle);
        }

        void DrawHud(float W, float H)
        {
            if (Player == null || Tracker == null) return;
            var d = Player.Damage;
            var g = Player.Gun;

            // Top-left: vehicle status.
            Panel(new Rect(10, 10, 300, 150));
            GUI.Label(new Rect(20, 14, 290, 26), Player.DisplayName + "   " + Mathf.RoundToInt(Mathf.Abs(Player.Controller.ForwardSpeedMs) * 3.6f) + " km/h", bodyStyle);
            Bar(new Rect(20, 46, 280, 14), d.Structure / Mathf.Max(1f, d.MaxStructure), "HULL", new Color(0.4f, 0.8f, 0.4f));
            Bar(new Rect(20, 66, 280, 14), g.ReloadProgress, g.Ready ? "READY" : "RELOADING", new Color(0.95f, 0.75f, 0.3f));
            string shell = g.Current != null ? g.Current.name + " (" + g.Current.ShortName + ")" : "-";
            GUI.Label(new Rect(20, 86, 290, 22), "Shell: " + shell + "   [Q]", smallStyle);
            GUI.Label(new Rect(20, 106, 290, 22), "Crew " + d.CrewAlive() + "/4" + (d.OnFire ? "   FIRE!" : ""), smallStyle);
            int lost = 0;
            foreach (var m in d.Modules) if (m.Destroyed) lost++;
            GUI.Label(new Rect(20, 126, 290, 22), lost > 0 ? lost + " module(s) out" : "All systems green", smallStyle);

            // Top-centre: mission status.
            string timer = Tracker.TimeRemaining >= 0 ? FormatTime(Tracker.TimeRemaining) : FormatTime(Tracker.Elapsed);
            Panel(new Rect(W * 0.5f - 150, 10, 300, 56));
            GUI.Label(new Rect(W * 0.5f - 140, 14, 280, 26), Mission.Title + "   " + timer, bodyStyle);
            GUI.Label(new Rect(W * 0.5f - 140, 38, 280, 22), "Enemies left: " + Tracker.EnemiesLeft + " / " + Tracker.EnemiesTotal, smallStyle);

            // Hit log.
            float ly = 180;
            foreach (var line in hitLog)
            {
                GUI.Label(new Rect(10, ly, W * 0.6f, 24), line, smallStyle);
                ly += 22;
            }

            // Reticle.
            Vector2 c = new Vector2(W * 0.5f, H * 0.5f);
            Color rc = Player.Turret.OnTarget ? new Color(0.4f, 1f, 0.4f, 0.9f) : new Color(1f, 1f, 1f, 0.7f);
            GUI.color = rc;
            if (Page == GameScreen.Playing && Rig.Mode == Cameras.CameraMode.GunnerSight)
            {
                DrawLine(c.x - 60, c.y, c.x - 14, c.y, 2); DrawLine(c.x + 14, c.y, c.x + 60, c.y, 2);
                DrawLine(c.x, c.y - 40, c.x, c.y - 12, 2); DrawLine(c.x, c.y + 12, c.x, c.y + 40, 2);
            }
            else
            {
                DrawLine(c.x - 14, c.y, c.x + 14, c.y, 2);
                DrawLine(c.x, c.y - 14, c.x, c.y + 14, 2);
            }
            GUI.color = Color.white;

            if (Player.Damage.IsDestroyed) GUI.Label(new Rect(0, H * 0.45f, W, 40), "DESTROYED", titleStyle);
            if (noticeTimer > 0f || !string.IsNullOrEmpty(Notice)) GUI.Label(new Rect(0, H * 0.3f, W, 30), Notice, bodyStyle);
            GUI.Label(new Rect(0, H - 30, W * 0.6f, 24), Rig.Mode == Cameras.CameraMode.GunnerSight ? "GUNNER SIGHT   [V] to exit" : "THIRD PERSON   [V] for gunner sight", smallStyle);
        }

        void DrawPause(float W, float H)
        {
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
            GUI.color = Color.white;
            Panel(new Rect(W * 0.5f - 170, H * 0.2f, 340, 340));
            GUI.Label(new Rect(W * 0.5f - 170, H * 0.2f + 14, 340, 40), "PAUSED", titleStyle);
            float x = W * 0.5f - 130, y = H * 0.2f + 80;
            if (Btn(new Rect(x, y, 260, 48), "Resume")) TogglePause();
            if (Btn(new Rect(x, y + 60, 260, 48), "Restart mission")) RestartMission();
            if (Btn(new Rect(x, y + 120, 260, 48), "Settings")) { settingsReturn = GameScreen.Paused; Page = GameScreen.Settings; }
            if (Btn(new Rect(x, y + 180, 260, 48), "Quit to menu")) ReturnToMenu();
        }

        void DrawResult(float W, float H)
        {
            Panel(new Rect(W * 0.5f - 230, H * 0.18f, 460, 360));
            bool won = Tracker.State == MissionState.Won;
            GUI.Label(new Rect(W * 0.5f - 230, H * 0.18f + 14, 460, 44), won ? "VICTORY" : "DEFEAT", titleStyle);
            string body = won
                ? "Time " + FormatTime(Tracker.Elapsed) + "\nKills " + Tracker.Kills + " / " + Tracker.EnemiesTotal + "\nCredits +" + (100 + Tracker.Kills * 50)
                : (Tracker.LossReason ?? "Mission failed.") + "\nKills " + Tracker.Kills + " / " + Tracker.EnemiesTotal;
            GUI.Label(new Rect(W * 0.5f - 200, H * 0.18f + 70, 400, 120), body, bodyStyle);
            float x = W * 0.5f - 130, y = H * 0.18f + 200;
            var next = NextMission();
            float row = 0f;
            if (won && next != null && IsUnlocked(next))
            {
                if (Btn(new Rect(x, y, 260, 46), "Next: " + next.Title)) StartMission(next.Id);
                row = 56f;
            }
            if (Btn(new Rect(x, y + row, 260, 46), "Retry mission")) RestartMission();
            if (Btn(new Rect(x, y + row + 56, 260, 46), "Back to menu")) ReturnToMenu();
        }

        MissionDefinition NextMission()
        {
            bool take = false;
            foreach (var m in MissionCatalog.All)
            {
                if (take) return m;
                if (m.Id == Mission.Id) take = true;
            }
            return null;
        }

        void Bar(Rect r, float frac, string label, Color c)
        {
            GUI.DrawTexture(r, barBgTex);
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(frac), r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 4, r.y - 4, r.width, r.height + 8), label, smallStyle);
        }

        void DrawLine(float x0, float y0, float x1, float y1, float thickness)
        {
            GUI.DrawTexture(new Rect(Mathf.Min(x0, x1), Mathf.Min(y0, y1), Mathf.Max(Mathf.Abs(x1 - x0), thickness), Mathf.Max(Mathf.Abs(y1 - y0), thickness)), Texture2D.whiteTexture);
        }

        static string FormatTime(float s)
        {
            int m = Mathf.FloorToInt(s / 60f), sec = Mathf.FloorToInt(s % 60f);
            return m.ToString("00") + ":" + sec.ToString("00");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WarForge.Core
{
    [Serializable]
    public class SettingsData
    {
        public float lookSensitivity = 1f;
        public bool invertY;
        public int quality = 1;          // 0 low, 1 medium, 2 high
        public float masterVolume = 0.8f;
        public bool ballisticAssist = true;
        public bool showFps;

        public void Sanitize()
        {
            lookSensitivity = Mathf.Clamp(lookSensitivity, 0.2f, 3f);
            quality = Mathf.Clamp(quality, 0, 2);
            masterVolume = Mathf.Clamp01(masterVolume);
        }
    }

    [Serializable]
    public class MissionRecord
    {
        public string missionId;
        public bool completed;
        public float bestTime;
        public int bestKills;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int credits;
        public string selectedVehicleId = "wf_warden";
        public SettingsData settings = new SettingsData();
        public List<MissionRecord> missions = new List<MissionRecord>();

        public MissionRecord Find(string missionId)
        {
            for (int i = 0; i < missions.Count; i++)
                if (missions[i] != null && missions[i].missionId == missionId) return missions[i];
            return null;
        }

        public MissionRecord GetOrCreate(string missionId)
        {
            var r = Find(missionId);
            if (r == null)
            {
                r = new MissionRecord { missionId = missionId };
                missions.Add(r);
            }
            return r;
        }

        public bool IsCompleted(string missionId)
        {
            var r = Find(missionId);
            return r != null && r.completed;
        }

        public void Sanitize()
        {
            if (settings == null) settings = new SettingsData();
            if (missions == null) missions = new List<MissionRecord>();
            missions.RemoveAll(m => m == null || string.IsNullOrEmpty(m.missionId));
            if (credits < 0) credits = 0;
            if (string.IsNullOrEmpty(selectedVehicleId)) selectedVehicleId = "wf_warden";
            settings.Sanitize();
        }
    }

    /// <summary>Local JSON save file in Application.persistentDataPath. No network, no account.</summary>
    public static class SaveSystem
    {
        const string FileName = "warforge_save.json";
        static SaveData data;

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Load()
        {
            data = null;
            try
            {
                if (File.Exists(FilePath)) data = FromJson(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WarForge] Save file could not be read, starting fresh: " + e.Message);
            }
            if (data == null) data = new SaveData();
            data.Sanitize();
        }

        public static void Save()
        {
            if (data == null) return;
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, ToJson(data));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WarForge] Save failed: " + e.Message);
            }
        }

        public static string ToJson(SaveData d) => JsonUtility.ToJson(d, true);

        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            var d = JsonUtility.FromJson<SaveData>(json);
            d?.Sanitize();
            return d;
        }
    }
}

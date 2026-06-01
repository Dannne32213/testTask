using UnityEngine;
using System.Collections.Generic;

public class SwitchPlatformService : IPlatformService
{
    public IAchievementService Achievements { get; private set; }
    public IPresenceService Presence { get; private set; }
    public IStorageService Storage { get; private set; }

    public void Initialize()
    {
        Achievements = new SwitchAchievementService();
        Storage = new SwitchStorageService();
        
        SwitchPrefs.Init();
        Debug.Log("[Platform] Switch Service Initialized.");
    }
}

public class SwitchAchievementService : IAchievementService
{
    public void UnlockAchievement(string achievementId) {
        
    }
    public void SetProgress(string achievementId, float progress) { }
    public List<AchievementStatus> GetLocalAchievements() => new List<AchievementStatus>();
}

public class SwitchStorageService : IStorageService
{
    public void SetInt(string key, int value) => SwitchPrefs.SetInt(key, value);
    public int GetInt(string key, int defaultValue = 0) => SwitchPrefs.GetInt(key, defaultValue);
    public void SetFloat(string key, float value) => SwitchPrefs.SetFloat(key, value);
    public float GetFloat(string key, float defaultValue = 0f) => SwitchPrefs.GetFloat(key, defaultValue);
    public void SetString(string key, string value) => SwitchPrefs.SetString(key, value);
    public string GetString(string key, string defaultValue = "") => SwitchPrefs.GetString(key, defaultValue);
    public bool HasKey(string key) => SwitchPrefs.HasKey(key);
    public void DeleteKey(string key) => SwitchPrefs.DeleteKey(key);
    public void Save() => SwitchPrefs.Save();
}

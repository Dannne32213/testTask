using UnityEngine;
using System.Collections.Generic;

public class StandalonePlatformService : IPlatformService
{
    public IAchievementService Achievements { get; private set; }
    public IPresenceService Presence { get; private set; }
    
    public IStorageService Storage { get; private set; }

    public void Initialize()
    {
        Achievements = new StandaloneAchievementService();
        Presence = new StandalonePresenceService();
        Storage = new StandaloneStorageService();
        
        Debug.Log("[Platform] Standalone Service Initialized.");
    }
}

public class StandaloneAchievementService : IAchievementService
{
    public void UnlockAchievement(string achievementId) => Debug.Log($"[Achievement] Unlocked: {achievementId}");
    public void SetProgress(string achievementId, float progress) => Debug.Log($"[Achievement] Progress: {achievementId} - {progress}%");
    public List<AchievementStatus> GetLocalAchievements() => new List<AchievementStatus>();
}

public class StandalonePresenceService : IPresenceService
{
    public void SetPresence(string status) => Debug.Log($"[Presence] Status set to: {status}");
}


public class StandaloneStorageService : IStorageService
{
    public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);
    public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
    public float GetFloat(string key, float defaultValue = 0f) => PlayerPrefs.GetFloat(key, defaultValue);
    public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
    public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);
    public bool HasKey(string key) => PlayerPrefs.HasKey(key);
    public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
    public void Save() => PlayerPrefs.Save();
}

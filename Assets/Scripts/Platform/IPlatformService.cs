using System.Collections.Generic;

public interface IPlatformService
{
    IAchievementService Achievements { get; }
    IPresenceService Presence { get; }
    
    IStorageService Storage { get; }
    
    void Initialize();
}

public interface IAchievementService
{
    void UnlockAchievement(string achievementId);
    void SetProgress(string achievementId, float progress);
    List<AchievementStatus> GetLocalAchievements();
}

public interface IPresenceService
{
    void SetPresence(string status);
}

public interface ISocialService
{
    // Extensibil pentru lista de prieteni, invitatii, etc.
    void ShowSocialUI();
}

public interface IStorageService
{
    void SetInt(string key, int value);
    int GetInt(string key, int defaultValue = 0);
    void SetFloat(string key, float value);
    float GetFloat(string key, float defaultValue = 0f);
    void SetString(string key, string value);
    string GetString(string key, string defaultValue = "");
    bool HasKey(string key);
    void DeleteKey(string key);
    void Save();
}

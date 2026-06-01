using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

public static class SaveSystem
{
    public static bool savesLoaded;
    public static PlayerData playerData;

    static string savePath = "save.SaveData";
    static int key = 5;

    public static bool HasSave()
    {
        if (!PlatformManager.Service.Storage.HasKey(savePath)) return false;
        
        if (playerData == null) LoadData();
        
        return playerData != null && playerData.hasPlayData;
    }
    
    public static void SaveData()
    {
        if (playerData == null) return;
        string json = JsonUtility.ToJson(playerData);
        PlatformManager.Service.Storage.SetString(savePath, EncryptDecrypt(json));
        PlatformManager.Service.Storage.Save();
        Debug.Log("Saved Data");
    }
    
    public static PlayerData LoadData()
    {
        if (savesLoaded && playerData != null) return playerData;

        playerData = new PlayerData();

        if (PlatformManager.Service.Storage.HasKey(savePath))
        {
            try
            {
                string json = PlatformManager.Service.Storage.GetString(savePath);
                playerData = JsonUtility.FromJson<PlayerData>(EncryptDecrypt(json));
            }
            catch
            {
                playerData = new PlayerData();
            }
        }

        InitAchievementsList();
        savesLoaded = true;

        return playerData;
    }

    private static void InitAchievementsList()
    {
        if (playerData.achievements == null) playerData.achievements = new List<AchievementStatus>();
        
        for (int i = 0; i < Enum.GetValues(typeof(ETrophey)).Length; i++)
        {
            if (playerData.achievements.Count < i)
            {
                playerData.achievements.Add(new AchievementStatus((ETrophey) i , 0f));
            }
        }
    }

    public static PlayerData MakeNewSave()
    {
        float currentVol = playerData != null ? playerData.masterVolume : 1.0f;

        playerData = new PlayerData();
        playerData.masterVolume = currentVol;
        playerData.hasPlayData = false; 

        PlatformManager.Service.Storage.DeleteKey(savePath);
        SaveData();
        Debug.Log("<color=red>New save created.</color>");
        return playerData;
    }

    private static string EncryptDecrypt(string textToEncrypt)
    {
        StringBuilder inSb = new StringBuilder(textToEncrypt);
        StringBuilder outSb = new StringBuilder(textToEncrypt.Length);
        char c;
        for (int i = 0; i < textToEncrypt.Length; i++)
        {
            c = inSb[i];
            c = (char)(c ^ key);
            outSb.Append(c);
        }
        return outSb.ToString();
    }

    public static byte[] CompressString(string jsonString)
    {
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
            }
            return memoryStream.ToArray();
        }
    }

    public static string DecompressString(byte[] compressedBytes)
    {
        using (var memoryStream = new MemoryStream(compressedBytes))
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                using (var outputStream = new MemoryStream())
                {
                    gzipStream.CopyTo(outputStream);
                    byte[] outputBytes = outputStream.ToArray();
                    return Encoding.UTF8.GetString(outputBytes);
                }
            }
        }
    }
}

[System.Serializable]
public class PlayerData
{
    public bool hasPlayData; 
    public bool newGameDone;
    public Color playerColor = Color.white;
    public int score;
    public float masterVolume = 1.0f;
    public List<AchievementStatus> achievements = new List<AchievementStatus>();
}

[Serializable]
public class AchievementStatus
{
    public ETrophey type;
    public float progressValue;
    public bool completed;

    public AchievementStatus(ETrophey achievementType, float f)
    {
        type = achievementType;
        progressValue = f;
    }
}

public enum ETrophey { FirstScore, HighScore, ColorChanger }

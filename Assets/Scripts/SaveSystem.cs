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
        return PlayerPrefs.HasKey(savePath);
    }
    
    public static void SaveData()
    {
        string json = JsonUtility.ToJson(playerData);
        PlayerPrefs.SetString(savePath, EncryptDecrypt(json));
        PlayerPrefs.Save();
        Debug.Log("Saved Data");
    }
    
    public static PlayerData LoadData()
    {
        playerData = new PlayerData();

        bool newSave = false;

        if (PlayerPrefs.HasKey(savePath))
        {
            try
            {
                string json = PlayerPrefs.GetString(savePath);
                playerData = JsonUtility.FromJson<PlayerData>(EncryptDecrypt(json));
            }
            catch
            {
                newSave = true;
            }
        }
        else
        {
            newSave = true;
        }

        InitAchievementsList();
        if (newSave)
        {
            SaveData();
        }

        if (playerData == null)
        {
            MakeNewSave();
        }
        savesLoaded = true;

        return playerData;
    }
    private static void InitAchievementsList()
    {
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
        if (playerData != null)
        {
            PlayerData tempData = playerData;
            playerData = new PlayerData();
            
            if (!playerData.newGameDone)
            {
                playerData.score = tempData.score;
                playerData.playerColor = tempData.playerColor;
            }
        }
        else
        {
            playerData = new PlayerData();
        }

        PlayerPrefs.DeleteKey(savePath);
        SaveData();
        Debug.Log("<color=red>Nu sunt Save-uri.</color>");
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
    public bool newGameDone;
    public Color playerColor;
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
public enum ETrophey
{
    
}
using UnityEngine;
using System.Runtime.InteropServices;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class SwitchPrefs
{
#if !UNITY_EDITOR && UNITY_SWITCH
    [DllImport("__Internal")] private static extern bool Native_SwitchPrefs_Init();
    [DllImport("__Internal")] private static extern void Native_SwitchPrefs_Save();
    [DllImport("__Internal")] private static extern bool Native_SwitchPrefs_HasKey(string key);
    [DllImport("__Internal")] private static extern void Native_SwitchPrefs_SetInt(string key, int value);
    [DllImport("__Internal")] private static extern int Native_SwitchPrefs_GetInt(string key, int defaultValue);
    [DllImport("__Internal")] private static extern void Native_SwitchPrefs_SetFloat(string key, float value);
    [DllImport("__Internal")] private static extern float Native_SwitchPrefs_GetFloat(string key, float defaultValue);
    [DllImport("__Internal")] private static extern void Native_SwitchPrefs_DeleteKey(string key);
    [DllImport("__Internal")] private static extern void Native_SwitchPrefs_DeleteAll();

    [DllImport("__Internal")] private static extern int Native_SwitchPrefs_GetStringLength(string key);
    [DllImport("__Internal")] private static extern void Native_SwitchPrefs_GetStringBuffer(string key, byte[] buffer);
    [DllImport("__Internal")] private static extern void Native_SwitchPrefs_SetStringBuffer(string key, byte[] buffer, int length);

        [DllImport("__Internal")] private static extern void Native_SwitchPrefs_GetUserHandle(ref nn.account.UserHandle handle);
#endif

    public static bool Init() =>
#if !UNITY_EDITOR && UNITY_SWITCH
        Native_SwitchPrefs_Init();
#else
        true;
#endif

    public static void SetInt(string key, int value)
    {
#if !UNITY_EDITOR && UNITY_SWITCH
        Native_SwitchPrefs_SetInt(key, value);
        PlayerPrefs.SetInt(key, value); // Oglindire pentru UHFPS
        SwitchSavePlayerPrefs.RequestSave();
#else
        PlayerPrefs.SetInt(key, value);
#endif
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
#if !UNITY_EDITOR && UNITY_SWITCH
        if (Native_SwitchPrefs_HasKey(key)) return Native_SwitchPrefs_GetInt(key, defaultValue);
        return PlayerPrefs.GetInt(key, defaultValue);
#else
        return PlayerPrefs.GetInt(key, defaultValue);
#endif
    }

    public static void SetFloat(string key, float value)
    {
#if !UNITY_EDITOR && UNITY_SWITCH
        Native_SwitchPrefs_SetFloat(key, value);
        PlayerPrefs.SetFloat(key, value); // Oglindire pentru UHFPS
        SwitchSavePlayerPrefs.RequestSave();
#else
        PlayerPrefs.SetFloat(key, value);
#endif
    }

    public static float GetFloat(string key, float defaultValue = 0f)
    {
#if !UNITY_EDITOR && UNITY_SWITCH
        if (Native_SwitchPrefs_HasKey(key)) return Native_SwitchPrefs_GetFloat(key, defaultValue);
        return PlayerPrefs.GetFloat(key, defaultValue);
#else
        return PlayerPrefs.GetFloat(key, defaultValue);
#endif
    }

    public static void SetString(string key, string value)
    {
#if !UNITY_EDITOR && UNITY_SWITCH
        if (value == null) value = "";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(value);
        Native_SwitchPrefs_SetStringBuffer(key, buffer, buffer.Length);
        PlayerPrefs.SetString(key, value); // Oglindire pentru UHFPS
        SwitchSavePlayerPrefs.RequestSave();
#else
        PlayerPrefs.SetString(key, value);
#endif
    }

    public static string GetString(string key, string defaultValue = "")
    {
#if !UNITY_EDITOR && UNITY_SWITCH
        if (Native_SwitchPrefs_HasKey(key)) {
            int len = Native_SwitchPrefs_GetStringLength(key);
            if (len > 0) {
                byte[] buffer = new byte[len];
                Native_SwitchPrefs_GetStringBuffer(key, buffer);
                return System.Text.Encoding.UTF8.GetString(buffer);
            }
        }
        return PlayerPrefs.GetString(key, defaultValue);
#else
        return PlayerPrefs.GetString(key, defaultValue);
#endif
    }

    public static bool HasKey(string key)
    {
#if !UNITY_EDITOR && UNITY_SWITCH
        if (Native_SwitchPrefs_HasKey(key)) return true;
        return PlayerPrefs.HasKey(key);
#else
        return PlayerPrefs.HasKey(key);
#endif
    }

    public static void DeleteKey(string key)
    {
#if !UNITY_EDITOR && UNITY_SWITCH
        Native_SwitchPrefs_DeleteKey(key);
        PlayerPrefs.DeleteKey(key);
        SwitchSavePlayerPrefs.RequestSave();
#else
        PlayerPrefs.DeleteKey(key);
#endif
    }

    public static void DeleteAll()
    {
#if !UNITY_EDITOR && UNITY_SWITCH
        Native_SwitchPrefs_DeleteAll();
        PlayerPrefs.DeleteAll();
        SwitchSavePlayerPrefs.RequestSave();
#else
        PlayerPrefs.DeleteAll();
#endif
    }

    public static void Save()
    {
#if !UNITY_EDITOR && UNITY_SWITCH
        SwitchSavePlayerPrefs.RequestSave();
#else
        PlayerPrefs.Save();
#endif
    }

    public static void ForcePhysicalSave()
    {
#if !UNITY_EDITOR && UNITY_SWITCH
        Native_SwitchPrefs_Save(); 
        
        // LINIA MAGICĂ: Aceasta confirma fisierele UHFPS pe SSD-ul consolei Switch!
        PlayerPrefs.Save();
#else
        PlayerPrefs.Save();
#endif
    }

#if !UNITY_EDITOR && UNITY_SWITCH
    public static void GetUserHandle(ref nn.account.UserHandle handle) {
        Native_SwitchPrefs_GetUserHandle(ref handle);
    }
#endif
}
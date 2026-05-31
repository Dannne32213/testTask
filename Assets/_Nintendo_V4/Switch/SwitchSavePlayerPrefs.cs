using System;
using UnityEngine;
using Newtonsoft.Json.Linq; 
using System.Globalization;

#if UNITY_SWITCH && !UNITY_EDITOR
using UnityEngine.Switch;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SwitchSavePlayerPrefs : MonoBehaviour {
    private static bool _isInitialised = false;
    public static Action onFocusStateChanged;
// --- SISTEMUL DE DEBOUNCING (LOTCHECK PASS) ---
    private static float _lastSaveTime = -10f;
    private static bool _savePending = false;
    private const float SAVE_COOLDOWN = 10f; // Max 20 de scrieri pe minut (limita e 32)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialise() {
        if (_isInitialised) return;
        GameObject main = new GameObject("SwitchSave_OmniManager");
        main.AddComponent<SwitchSavePlayerPrefs>();
        DontDestroyOnLoad(main);
        _isInitialised = true;
    }
    
    public static void RequestSave() {
        _savePending = true;
    }

    // Aici are loc controlul frecvenței scrierilor pe disk
    void Update() {
        //Am adaugat UnityEngine.Time ca sa evitam conflictul cu namespace-ul Switch!
        if (_savePending && (UnityEngine.Time.unscaledTime - _lastSaveTime >= SAVE_COOLDOWN)) {
            ExecutePhysicalSave();
        }
    }

    // Scrierea reală pe hardware
    private static void ExecutePhysicalSave() {
        _savePending = false;
        _lastSaveTime = UnityEngine.Time.unscaledTime; // Explicit UnityEngine.Time
        SwitchPrefs.ForcePhysicalSave();
        // Debug.Log("[SwitchPrefs] Datele au fost scrise fizic pe disk (Debounced).");
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void LastInitialise() {
#if UNITY_SWITCH && !UNITY_EDITOR
        Notification.EnterExitRequestHandlingSection();
#endif
    }

    void OnEnable() {
#if UNITY_SWITCH && !UNITY_EDITOR
        if (SwitchPrefs.Init()) {
            // Rulăm migrarea pentru datele native Unity (Setările tale)
            MigrateNativePlayerPrefs();
            
            // Rulăm migrarea pentru JSON (pentru alte proiecte)
            MigrateV0Saves();
        }
        Notification.notificationMessageReceived += OnNotificationReceived;
        
        Notification.SetPerformanceModeChangedNotificationEnabled(true);
        Notification.SetResumeNotificationEnabled(true);
        Notification.SetOperationModeChangedNotificationEnabled(true);
#endif
    }

#if UNITY_SWITCH && !UNITY_EDITOR
    private void OnNotificationReceived(Notification.Message message) {
        switch (message) {
            case Notification.Message.ExitRequest:
                // 2. Acum că RAM-ul are JSON-ul cel nou, îl dăm spre C++ să-l scrie fizic!
                ExecutePhysicalSave();
                // 3. Spunem consolei Switch că am terminat și poate închide jocul.
                Notification.LeaveExitRequestHandlingSection();
                break;

            case Notification.Message.FocusStateChanged:
                // Când jucătorul pune jocul în bară (apăsând HOME, fără să-l închidă)

                if (UnityEngine.Time.unscaledTime - _lastSaveTime >= 2.5f) {
                    ExecutePhysicalSave(); 
                }
                onFocusStateChanged?.Invoke();
                break;

            case Notification.Message.Resume:
            case Notification.Message.OperationModeChanged:
            case Notification.Message.PerformanceModeChanged:
                break;
        }
    }
#endif

    // =========================================================
    // 1. MIGRARE PENTRU CHEILE NATIVE PLAYERPREFS (100% AUTOMAT)
    // =========================================================
    private void MigrateNativePlayerPrefs() {
        bool needsSave = false;
        
        // Verificăm dacă C++ a găsit vreo cheie și ne-a trimis lista
        if (SwitchPrefs.HasKey("OMNI_MASTER_KEY_LIST")) {
            
            // Citim lista (ex: "Sensitivity|Volume|Vibration|")
            string rawList = SwitchPrefs.GetString("OMNI_MASTER_KEY_LIST");
            
            // O tăiem în cuvinte separate
            string[] discoveredKeys = rawList.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string key in discoveredKeys) {
                string autoKeyFromCpp = "V0_AUTO_" + key;

                // Dacă valoarea există în memorie, o mutăm
                if (SwitchPrefs.HasKey(autoKeyFromCpp)) {
                    int extractedValue = SwitchPrefs.GetInt(autoKeyFromCpp);
                    
                    // O salvăm curat
                    SwitchPrefs.SetInt(key, extractedValue);
                    Debug.Log($"[OmniMigrator] FULL-AUTO EXTRACT: {key} = {extractedValue}");
                    
                    // Ștergem gunoiul temporar
                    SwitchPrefs.DeleteKey(autoKeyFromCpp);
                    needsSave = true;
                }
            }
            
            // Ștergem lista master ca să nu ocupe spațiu pe disk
            SwitchPrefs.DeleteKey("OMNI_MASTER_KEY_LIST");
        }

        if (needsSave) {
            SwitchPrefs.Save();
            Debug.Log("[OmniMigrator] Migrarea setărilor native 100% automată s-a încheiat.");
        }
    }

    // =========================================================
    // 2. MIGRARE PENTRU BLOCURI JSON (ALTE PROIECTE)
    // =========================================================
    private void MigrateV0Saves() {
        bool needsSave = false;

        for (int i = 0; i < 20; i++) {
            string tempKey = "V0_MIGRATION_BLOCK_" + i;
            
            if (SwitchPrefs.HasKey(tempKey)) {
                string rawData = SwitchPrefs.GetString(tempKey);
                try {
                    JObject parsedData = JObject.Parse(rawData);
                    Debug.Log($"[OmniMigrator] Procesăm automat datele din {tempKey}...");

                    // 1. OGLINDIRE AUTOMATĂ (Mirroring)
                    foreach (var kvp in parsedData) {
                        string key = kvp.Key;
                        string value = (kvp.Value.Type == JTokenType.String) 
                                       ? (string)kvp.Value 
                                       : kvp.Value.ToString(Newtonsoft.Json.Formatting.None);

                        SwitchPrefs.SetString(key, value);
                        
                        AutoMapAudio(key, value);
                    }

                    // 2. AUTO-ÎMPACHETARE (Packaging)
                    string[] systemKeys = { "PlayerData", "SaveGameData", "AllSaveData", "SaveData" , "save.data" };
                    foreach (string sKey in systemKeys) {
                        SwitchPrefs.SetString(sKey, rawData);
                    }

                    needsSave = true;
                    SwitchPrefs.DeleteKey(tempKey);
                } 
                catch (Exception e) {
                    Debug.LogError($"[OmniMigrator] Eroare la procesare: {e.Message}");
                }
            }
        }
        
        if (needsSave) {
            SwitchPrefs.Save();
            Debug.Log("[OmniMigrator] Migrare completă și automatizată reușită.");
        }
    }

    private void AutoMapAudio(string key, string value) {
        string k = key.ToLower();
        if (k.Contains("music") || k.Contains("master")) SwitchPrefs.SetString("MasterVolume", value);
        if (k.Contains("effect") || k.Contains("siren")) SwitchPrefs.SetString("EffectVolume", value);
        
        if (!SwitchPrefs.HasKey("AudioOptions")) {
            string audioJson = $@"{{""MasterVolume"":{value},""BackgroundVolume"":1.0,""MusicVolume"":{value},""EffectVolume"":1.0,""SirenVolume"":1.0,""DialogVolume"":1.0,""UiVolume"":1.0}}";
            SwitchPrefs.SetString("AudioOptions", audioJson);
        }
    }
    
#if UNITY_EDITOR
    public class SwitchPrefsEditorWindow : EditorWindow
    {
        // Creăm un element nou în meniul de sus al Unity-ului
        [MenuItem("Window/Switch Prefs Manager")]
        public static void ShowWindow()
        {
            // Deschidem fereastra
            GetWindow<SwitchPrefsEditorWindow>("Switch Prefs");
        }

        private void OnGUI()
        {
            GUILayout.Label("Switch Save Data Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Adăugăm un mesaj de avertizare/informațional
            EditorGUILayout.HelpBox("Acest buton va șterge toate salvările locale (PlayerPrefs) din Editor.", MessageType.Info);
        
            EditorGUILayout.Space();

            // Creăm butonul "Clear Save"
            if (GUILayout.Button("Clear All Saves", GUILayout.Height(40)))
            {
                // Adăugăm un dialog de confirmare pentru a preveni ștergerile accidentale
                if (EditorUtility.DisplayDialog("Clear Save Data", 
                        "Ești sigur că vrei să ștergi toate salvările? Această acțiune nu poate fi anulată.", 
                        "Da, Șterge", "Anulează"))
                {
                    SwitchPrefs.DeleteAll();
                    SwitchPrefs.Save();
                    Debug.Log("<b>[SwitchPrefs]</b> Toate salvările au fost șterse cu succes.");
                }
            }
        }
    }
#endif
}
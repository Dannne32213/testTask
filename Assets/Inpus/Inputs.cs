using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public enum ControlScheme
{
    PC,
    Console
}

public class InputMapContext
{
    public string MapName;
    public int Priority;
}

public class Inputs : MonoBehaviour
{
    public static Inputs Instance { get; private set; }

    [Header("Settings")] [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private bool showDebugOverlay = true;

    public event Action<ControlScheme> OnSchemeChanged;
    public ControlScheme CurrentScheme { get; private set; } = ControlScheme.PC;

    public bool JustSwitched { get; private set; }
    private int _lastSwitchFrame = -1;

    private List<InputMapContext> _activeMaps = new List<InputMapContext>();

    public bool IsMouseActive { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (inputActionAsset == null) return;

        foreach (var map in inputActionAsset.actionMaps)
        {
            map.actionTriggered += OnActionTriggered;
        }

        ApplyCursorStateForScheme();
        EnableMap("UI", 0);
    }

    private void OnDestroy()
    {
        if (inputActionAsset != null)
        {
            foreach (var map in inputActionAsset.actionMaps)
            {
                map.actionTriggered -= OnActionTriggered;
            }
        }
    }

    private float _lastSwitchTime = 0f;
    private const float SwitchCooldown = 0.5f;

    private void Update()
    {
    }

    private void LateUpdate()
    {
        if (JustSwitched && Time.frameCount != _lastSwitchFrame)
        {
            JustSwitched = false;
        }
    }

    private void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (context.action == null || context.control == null) return;

        // Resetăm JustSwitched dacă am trecut la un alt frame
        if (Time.frameCount != _lastSwitchFrame) JustSwitched = false;

        if (Time.unscaledTime - _lastSwitchTime < SwitchCooldown) return;

        // doar când o acțiune a început sau a fost executată
        if (!context.started && !context.performed) return;

        InputDevice device = context.control.device;
        bool isGamepad = device is Gamepad;
        bool isPC = device is Keyboard || device is Mouse;

        if (isGamepad)
        {
            // Verificăm dacă este o mișcare analogică
            if (context.action.type == InputActionType.Value)
            {
                var value = context.ReadValueAsObject();
                float magnitude = 0f;
                if (value is Vector2 v) magnitude = v.magnitude;
                else if (value is float f) magnitude = Mathf.Abs(f);

                if (magnitude < 0.25f) return;
            }

            IsMouseActive = false;
            if (CurrentScheme != ControlScheme.Console)
            {
                SwitchScheme(ControlScheme.Console);
                _lastSwitchTime = Time.unscaledTime;
                _lastSwitchFrame = Time.frameCount;
                JustSwitched = true;
            }
        }
        else if (isPC)
        {
            if (device is Mouse mouse)
            {
                if (mouse.delta.ReadValue().magnitude < 0.5f) return;
                
                bool wasMouseActive = IsMouseActive;
                IsMouseActive = true;

                if (CurrentScheme != ControlScheme.PC)
                {
                    SwitchScheme(ControlScheme.PC);
                    _lastSwitchTime = Time.unscaledTime;
                    _lastSwitchFrame = Time.frameCount;
                    JustSwitched = true;
                }
                else if (!wasMouseActive)
                {
                    // Dacă eram pe tastatură și am mișcat mouse-ul, deselectăm pentru a ascunde selectorul
                    if (UnityEngine.EventSystems.EventSystem.current != null)
                    {
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                    }
                }
            }
            else if (device is Keyboard)
            {
                IsMouseActive = false;
                if (CurrentScheme != ControlScheme.PC)
                {
                    SwitchScheme(ControlScheme.PC);
                    _lastSwitchTime = Time.unscaledTime;
                    _lastSwitchFrame = Time.frameCount;
                    JustSwitched = true;
                }
                
                // Când apăsăm pe tastatură, activăm selectorul
                RestoreUISelection();
            }
        }
    }


    private void SwitchScheme(ControlScheme newScheme)
    {
        CurrentScheme = newScheme;
        ApplyCursorStateForScheme();
        
        // Dacă am schimbat schema (JustSwitched), dezactivăm temporar EventSystem
        // pentru a preveni ca primul click să activeze un buton de UI în același frame.
        if (JustSwitched && UnityEngine.EventSystems.EventSystem.current != null)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            eventSystem.enabled = false;
            // Îl reactivăm imediat în frame-ul următor
            StartCoroutine(ReEnableEventSystem(eventSystem));
        }

        if (newScheme == ControlScheme.Console)
        {
            RestoreUISelection();
        }
        else
        {
            // Dacă am trecut pe PC, verificăm dacă e mouse sau tastatură
            // Dacă e mouse (IsMouseActive e deja setat în OnActionTriggered), deselectăm
            if (IsMouseActive && UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
        }

        OnSchemeChanged?.Invoke(CurrentScheme);
        Debug.LogWarning($"[Inputs] SWITCHED SCHEME TO: {CurrentScheme} (Interaction blocked for activation frame)");
    }

    private System.Collections.IEnumerator ReEnableEventSystem(UnityEngine.EventSystems.EventSystem es)
    {
        yield return null; 
        if (es != null) es.enabled = true;
    }

    private void RestoreUISelection()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return;
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem.currentSelectedGameObject == null)
        {
            if (WindowManager.Instance != null)
            {
                WindowManager.Instance.RestoreSelection();
            }
        }
    }

    private void ApplyCursorStateForScheme()
    {
        if (CurrentScheme == ControlScheme.PC)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void OnGUI()
    {
        GUI.depth = -2000;
        Rect areaRect = new Rect(10, 10, 300, 150);
        GUI.Box(areaRect, "");
        GUI.Box(areaRect, "<b><size=18><color=yellow> INPUT SYSTEM DEBUG (v2) </color></size></b>");
        GUILayout.BeginArea(new Rect(20, 40, 480, 200));
        GUILayout.Space(10);
        string schemeColor = CurrentScheme == ControlScheme.PC ? "lime" : "cyan";
        GUILayout.Label($"<b><size=22>CURRENT SCHEME: <color={schemeColor}>{CurrentScheme.ToString().ToUpper()}</color></size></b>");
        GUILayout.Space(5);
        GUILayout.Label($"<size=16>Cursor: {(Cursor.visible ? "Visible" : "Hidden")} | Locked: {Cursor.lockState}</size>");
        if (_activeMaps.Count > 0)
        {
            int maxPrio = _activeMaps.Max(m => m.Priority);
            var active = _activeMaps.Find(m => m.Priority == maxPrio);
            GUILayout.Label($"<size=16>Active Map: <color=white>{active?.MapName}</color> (Prio: {active?.Priority})</size>");
        }
        GUILayout.EndArea();
    }

    public void ClearAllActiveMaps()
    {
        _activeMaps.Clear();
        EvaluateActiveMaps();
    }

    public void EnableMap(string mapName, int priority)
    {
        var existing = _activeMaps.FirstOrDefault(m => m.MapName == mapName);
        if (existing != null) existing.Priority = priority;
        else _activeMaps.Add(new InputMapContext { MapName = mapName, Priority = priority });
        EvaluateActiveMaps();
    }

    public void DisableMap(string mapName)
    {
        _activeMaps.RemoveAll(m => m.MapName == mapName);
        EvaluateActiveMaps();
    }

    private void EvaluateActiveMaps()
    {
        if (inputActionAsset == null) return;
        if (_activeMaps.Count == 0)
        {
            foreach (var map in inputActionAsset.actionMaps) map.Disable();
            return;
        }
        int maxPrio = _activeMaps.Max(m => m.Priority);
        var toEnable = _activeMaps.Where(m => m.Priority == maxPrio).Select(m => m.MapName).ToList();
        foreach (var map in inputActionAsset.actionMaps)
        {
            if (toEnable.Contains(map.name)) map.Enable();
            else map.Disable();
        }
    }

    public Vector2 MoveInput => ReadVector2("Move");
    public Vector2 MouseInput => ReadVector2("Point");
    public bool BackPressed => WasTriggered("Back");
    public bool PausePressed => WasTriggered("Pause");
    public bool ConfirmPressed => WasTriggered("Confirm");

    public InputAction GetAction(string name)
    {
        if (inputActionAsset == null) return null;
        foreach (var map in inputActionAsset.actionMaps)
        {
            if (map.enabled)
            {
                var action = map.FindAction(name);
                if (action != null) return action;
            }
        }
        return null;
    }

    public Vector2 ReadVector2(string name) => GetAction(name)?.ReadValue<Vector2>() ?? Vector2.zero;
    
    public bool WasTriggered(string name)
    {
        var action = GetAction(name);
        return action?.triggered ?? false;
    }
    public bool IsPressed(string name) => GetAction(name)?.IsPressed() ?? false;
}

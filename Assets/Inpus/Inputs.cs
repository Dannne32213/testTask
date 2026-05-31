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

    [Header("Settings")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private bool showDebugOverlay = true;

    public event Action<ControlScheme> OnSchemeChanged;
    public ControlScheme CurrentScheme { get; private set; } = ControlScheme.PC;

    private List<InputMapContext> _activeMaps = new List<InputMapContext>();

    private void Awake()
    {
        
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (inputActionAsset == null)
        {
            return;
        }

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

    private void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (context.action == null || context.control == null) return;
        
        if (Time.unscaledTime - _lastSwitchTime < SwitchCooldown) return;

        if (!context.started && !context.performed) return;

        InputDevice device = context.control.device;
        bool isGamepad = device is Gamepad;
        bool isPC = device is Keyboard || device is Mouse;

        if (isGamepad)
        {
            if (context.action.type == InputActionType.Value)
            {
                var value = context.ReadValueAsObject();
                float magnitude = 0f;
                if (value is Vector2 v) magnitude = v.magnitude;
                else if (value is float f) magnitude = Mathf.Abs(f);

                if (magnitude < 0.25f) return;
            }

            if (CurrentScheme != ControlScheme.Console)
            {
                SwitchScheme(ControlScheme.Console);
                _lastSwitchTime = Time.unscaledTime;
            }
        }
        else if (isPC)
        {
            if (context.action.name == "Point") return;

            if (context.action.name == "Move" && device is Mouse)
            {
                if (Mouse.current.delta.ReadValue().magnitude < 2.0f) return;
            }

            if (CurrentScheme != ControlScheme.PC)
            {
                SwitchScheme(ControlScheme.PC);
                _lastSwitchTime = Time.unscaledTime;
            }
        }
    }

    private void SwitchScheme(ControlScheme newScheme)
    {
        CurrentScheme = newScheme;
        ApplyCursorStateForScheme();

        if (newScheme == ControlScheme.Console)
        {
            RestoreUISelection();
        }

        OnSchemeChanged?.Invoke(CurrentScheme);
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
        
        Rect areaRect = new Rect(10, 10, 500, 250);
        GUI.Box(areaRect, ""); // Shadow/Background
        GUI.Box(areaRect, "<b><size=18><color=yellow> INPUT SYSTEM DEBUG (v2) </color></size></b>");

        GUILayout.BeginArea(new Rect(20, 40, 480, 200));
        
        GUILayout.Space(10);
        string schemeColor = CurrentScheme == ControlScheme.PC ? "lime" : "cyan";
        GUILayout.Label($"<b><size=22>CURRENT SCHEME: <color={schemeColor}>{CurrentScheme.ToString().ToUpper()}</color></size></b>");
        
        GUILayout.Space(5);
        GUILayout.Label($"<size=16>Cursor: {(Cursor.visible ? "Visible" : "Hidden")} | Locked: {Cursor.lockState}</size>");
        GUILayout.Label($"<size=16>Blocked Input: <color=orange>{(CurrentScheme == ControlScheme.PC ? "Gamepad" : "Keyboard/Mouse")}</color></size>");

        if (_activeMaps.Count > 0)
        {
            int maxPrio = _activeMaps.Max(m => m.Priority);
            var active = _activeMaps.Find(m => m.Priority == maxPrio);
            GUILayout.Label($"<size=16>Active Map: <color=white>{active?.MapName}</color> (Prio: {active?.Priority})</size>");
        }
        else
        {
            GUILayout.Label("<color=red>NO ACTIVE MAPS!</color>");
        }

        GUILayout.EndArea();
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
            if (toEnable.Contains(map.name))
            {
                map.Enable();
            }
            else
            {
                map.Disable();
            }
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
        bool triggered = action?.triggered ?? false;
        if (triggered) Debug.Log($"[Inputs] Action TRIGGERED: {name}");
        return triggered;
    }
    public bool IsPressed(string name) => GetAction(name)?.IsPressed() ?? false;
}

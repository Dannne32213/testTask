using UnityEngine;
using UnityEngine.InputSystem;

public class Inputs : MonoBehaviour
{
    public static Inputs Instance { get; private set; }

    [SerializeField] private InputActionAsset inputActionAsset;

    private InputAction _moveAction;
    private InputAction _mouseAction;
    private InputAction _backAction;
    private InputAction _confirmAction;

    public Vector2 MoveInput => _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
    public Vector2 MouseInput => _mouseAction?.ReadValue<Vector2>() ?? Vector2.zero;
    
    public bool BackPressed => _backAction?.triggered ?? false;
    public bool ConfirmPressed => _confirmAction?.triggered ?? false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeActions();
    }

    private void InitializeActions()
    {
        if (inputActionAsset != null)
        {
            var uiMap = inputActionAsset.FindActionMap("UI");
            if (uiMap != null)
            {
                _moveAction = uiMap.FindAction("Move");
                _mouseAction = uiMap.FindAction("Mouse");
                _backAction = uiMap.FindAction("Back");
                _confirmAction = uiMap.FindAction("Confirm");

                uiMap.Enable();
               
            }
            
        }
    
    }
}

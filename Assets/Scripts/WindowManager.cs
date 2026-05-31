using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WindowManager : MonoBehaviour
{
    public static WindowManager Instance { get; private set; }

    [SerializeField] private WindowBase defaultWindow;

    [SerializeField] private Stack<WindowBase> _windowStack = new Stack<WindowBase>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (defaultWindow != null)
        {
            OpenWindow(defaultWindow);
        }
    }

    public WindowBase GetTopWindow()
    {
        return _windowStack.Count > 0 ? _windowStack.Peek() : null;
    }

    public void RestoreSelection()
    {
        if (_windowStack.Count > 0)
        {
            _windowStack.Peek().RestoreFocus();
        }
    }

    public void OpenWindow(WindowBase newWindow)
    {
        if (newWindow == null) return;
        
        if (_windowStack.Count > 0)
        {
            WindowBase currentTop = _windowStack.Peek();
            currentTop.Freeze(); 
        }
        
        _windowStack.Push(newWindow);
        newWindow.Show();

        if (Inputs.Instance != null)
        {
            Inputs.Instance.EnableMap("UI", 10);
            Inputs.Instance.DisableMap("Gameplay");
        }
    }

    public void CloseTopWindow()
    {
        if (_windowStack.Count <= 0) return;

        WindowBase topWindow = _windowStack.Pop();
        topWindow.Hide();

        if (_windowStack.Count > 0)
        {
            WindowBase previousWindow = _windowStack.Peek();
            previousWindow.Show(); 
            previousWindow.RestoreFocus();
        }
        else
        {
            if (Inputs.Instance != null)
            {
                Inputs.Instance.DisableMap("UI");
                Inputs.Instance.EnableMap("Gameplay", 10);
            }
        }
    }

    private void Update()
    {
        if (Inputs.Instance == null) return;

        if (Inputs.Instance.ConfirmPressed)
        {
            if (_windowStack.Count > 0)
            {
                GameObject targetObject = null;

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current)
                    {
                        position = Mouse.current.position.ReadValue()
                    };

                    List<RaycastResult> results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, results);

                    foreach (var result in results)
                    {
                        if (result.gameObject.GetComponent<Selectable>() != null && result.gameObject.transform.IsChildOf(_windowStack.Peek().transform))
                        {
                            targetObject = result.gameObject;
                            break;
                        }
                    }
                }
                else
                {
                    targetObject = EventSystem.current.currentSelectedGameObject;
                }
                
                if (targetObject == null)
                {
                    _windowStack.Peek().RestoreFocus();
                    return; 
                }

                Button btn = targetObject.GetComponent<Button>();
                if (btn != null && btn.interactable)
                {
                    btn.onClick.Invoke();
                }
                else
                {
                    ExecuteEvents.Execute(targetObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }
            }
        }

        if (Inputs.Instance.BackPressed)
        {
            if (_windowStack.Count > 1) 
            {
                CloseTopWindow();
            }
        }
    }
}

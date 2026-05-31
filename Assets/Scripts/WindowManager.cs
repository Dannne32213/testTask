using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WindowManager : MonoBehaviour
{
    public static WindowManager Instance { get; private set; }

    [SerializeField] private WindowBase defaultWindow;
    
    [SerializeField] private List<WindowBase> _windowStack = new List<WindowBase>();

    private void Awake()
    {
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
        return _windowStack.Count > 0 ? _windowStack[_windowStack.Count - 1] : null;
    }

    public void RestoreSelection()
    {
        WindowBase top = GetTopWindow();
        if (top != null)
        {
            top.RestoreFocus();
        }
    }

    public void OpenWindow(WindowBase newWindow)
    {
        if (newWindow == null) return;
        
        WindowBase currentTop = GetTopWindow();
        if (currentTop != null)
        {
            currentTop.Freeze();
        }
        
        _windowStack.Add(newWindow);
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

        int lastIndex = _windowStack.Count - 1;
        WindowBase topWindow = _windowStack[lastIndex];
        _windowStack.RemoveAt(lastIndex);
        
        topWindow.Hide();

        WindowBase newTop = GetTopWindow();
        if (newTop != null)
        {
            newTop.Show(); 
            newTop.RestoreFocus();
        }
        else
        {
            if (Inputs.Instance != null)
            {
                Inputs.Instance.DisableMap("UI");
                Inputs.Instance.EnableMap("Gameplay", 10);
                Inputs.Instance.EnableMap("Pausepressed", 10);
                Debug.Log("[WindowManager] Toate ferestrele închise. Re-activăm Gameplay și Pausepressed.");
            }
        }
    }

    private void Update()
    {
        if (Inputs.Instance == null) return;

        if (Inputs.Instance.JustSwitched) return;
        
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            WindowBase top = GetTopWindow();
            if (top != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Mouse.current.position.ReadValue()
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                foreach (var result in results)
                {
                    if (result.gameObject.GetComponent<Selectable>() != null && result.gameObject.transform.IsChildOf(top.transform))
                    {
                        if (EventSystem.current.currentSelectedGameObject != result.gameObject)
                        {
                            EventSystem.current.SetSelectedGameObject(result.gameObject);
                        }
                        break;
                    }
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WindowManager : MonoBehaviour
{
    public static WindowManager Instance { get; private set; }

    [SerializeField] private WindowBase defaultWindow;

    private Stack<WindowBase> _windowStack = new Stack<WindowBase>();

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
        // Deschidem fereastra default (ex: Main Menu) la pornire și o punem în stivă
        if (defaultWindow != null)
        {
            Debug.Log($"[WindowManager] Opening default window: {defaultWindow.name}");
            OpenWindow(defaultWindow);
        }
    }

    public void OpenWindow(WindowBase newWindow)
    {
        if (newWindow == null) return;

        // Dacă avem deja o fereastră deschisă, o ascundem
        if (_windowStack.Count > 0)
        {
            WindowBase currentTop = _windowStack.Peek();
            Debug.Log($"[WindowManager] Hiding current window: {currentTop.name}");
            currentTop.Hide();
        }
        // Punem noua fereastră în stivă și o afișăm
        _windowStack.Push(newWindow);
        Debug.Log($"[WindowManager] Pushed to stack: {newWindow.name}. Stack size: {_windowStack.Count}");
        newWindow.Show();
    }

    public void CloseTopWindow()
    {
        Debug.Log($"[WindowManager] CloseTopWindow called. Stack count: {_windowStack.Count}");
        if (_windowStack.Count <= 1)
        {
            Debug.Log("[WindowManager] Cannot close the last window in stack.");
            return;
        }

        WindowBase topWindow = _windowStack.Pop();
        Debug.Log($"[WindowManager] Closing window: {topWindow.name}");
        topWindow.Hide();

        if (_windowStack.Count > 0)
        {
            WindowBase previousWindow = _windowStack.Peek();
            Debug.Log($"[WindowManager] Re-opening previous window: {previousWindow.name}");
            previousWindow.Show();
        }
    }

    private void Update()
    {
        if (Inputs.Instance == null) return;

        // Gestionare buton CONFIRM
        if (Inputs.Instance.ConfirmPressed)
        {
            if (_windowStack.Count > 0)
            {
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                
                if (selected == null)
                {
                    Debug.LogWarning("[WindowManager] Focus lost. Restoring...");
                    _windowStack.Peek().Show();
                    return; 
                }

                Button btn = selected.GetComponent<Button>();
                if (btn != null && btn.interactable)
                {
                    btn.onClick.Invoke();
                }
                else
                {
                    ExecuteEvents.Execute(selected, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }
            }
        }

        // Gestionare buton BACK
        if (Inputs.Instance.BackPressed)
        {
            Debug.Log($"[WindowManager] Back Pressed. Stack count: {_windowStack.Count}");
            if (_windowStack.Count > 1) 
            {
                CloseTopWindow();
            }
            else
            {
                Debug.LogWarning("[WindowManager] Back pressed but stack count is 1 or less. Nothing to go back to.");
            }
        }
    }
}

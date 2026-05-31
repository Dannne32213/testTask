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
            currentTop.Hide();
        }
        
        _windowStack.Push(newWindow);
        newWindow.Show();
    }

    public void CloseTopWindow()
    {
        if (_windowStack.Count <= 1)
        {
            return;
        }

        WindowBase topWindow = _windowStack.Pop();
        topWindow.Hide();

        if (_windowStack.Count > 0)
        {
            WindowBase previousWindow = _windowStack.Peek();
            previousWindow.Show();
        }
    }

    private void Update()
    {
        if (Inputs.Instance == null) return;

        if (Inputs.Instance.ConfirmPressed)
        {
            if (_windowStack.Count > 0)
            {
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                
                if (selected == null)
                {
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

        if (Inputs.Instance.BackPressed)
        {
            if (_windowStack.Count > 1) 
            {
                CloseTopWindow();
            }
           
        }
    }
}

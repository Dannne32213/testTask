using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class WindowBase : MonoBehaviour
{
    [SerializeField] private Selectable _firstSelectedGamepad;

    private CanvasGroup _canvasGroup;
    private GameObject _lastSelected;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(SelectLater());
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }
    }

    //aici ne asiguram ca avem macar ceva selectat cind facem switch intre gamepad/mouse/tastatura
    private IEnumerator SelectLater()
    {
        yield return null;

        if (EventSystem.current == null) yield break;

        GameObject objectToSelect = null;
        
        if (_lastSelected != null && _lastSelected.activeInHierarchy)
        {
            objectToSelect = _lastSelected;
        }
        else if (_firstSelectedGamepad != null)
        {
            objectToSelect = _firstSelectedGamepad.gameObject;
        }

        if (objectToSelect != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(objectToSelect);
        }
    }

    private void Update()
    {
        if (EventSystem.current == null) return;

        //fereastra currenta, nui da voie la celeante sa fie interectable
        if (WindowManager.Instance != null && WindowManager.Instance.GetTopWindow() != this)
        {
            return;
        }

        //aici alege ultimul button dupa inchiderea ferestrei
        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current != null && current.transform.IsChildOf(this.transform))
        {
            _lastSelected = current;
        }

        //aici tot ce e legat de mouse, adica tine mouseul deasupra ferestrei daca e pe pc
        if (Inputs.Instance != null && Inputs.Instance.CurrentScheme == ControlScheme.PC)
        {
            Vector2 mousePos = Vector2.zero; // exact locatia cursorului
            if (Mouse.current != null)
            {
                mousePos = Mouse.current.position.ReadValue();
            }

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = mousePos
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            bool foundHover = false;
            foreach (var result in results)
            {
                Selectable selectable = result.gameObject.GetComponentInParent<Selectable>();
                if (selectable != null && selectable.interactable && selectable.transform.IsChildOf(this.transform))
                {
                    foundHover = true;
                    break;
                }
            }

            // DACĂ MOUSE-UL NU ESTE PESTE NIMIC, DESELECTĂM TOTUL (Specific pentru PC)
            // Asta previne ca un buton să rămână "selectat" vizual dacă mouse-ul a plecat de pe el
            // Facem asta DOAR dacă mouse-ul este dispozitivul activ
            if (Inputs.Instance.IsMouseActive && !foundHover && EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    public virtual void Freeze()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    public void RestoreFocus()
    {
        StopAllCoroutines();
        StartCoroutine(SelectLater());
    }
}

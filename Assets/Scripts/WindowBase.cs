using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
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

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current != null && current.transform.IsChildOf(this.transform))
        {
            _lastSelected = current;
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class DefaultSelect : MonoBehaviour
{

    [SerializeField] private GameObject DefaultButton;
     private EventSystem _eventSystem;
    // Start is called before the first frame update
    void Start()
    {
        _eventSystem = EventSystem.current;
    }

    // Update is called once per frame
    void Update()
    {
        if(_eventSystem.currentSelectedGameObject==null || !_eventSystem.currentSelectedGameObject.activeInHierarchy) {
            _eventSystem.SetSelectedGameObject(DefaultButton);
        }
        
    }
    
    
    
    
    
    
    
    
    
    
    
}

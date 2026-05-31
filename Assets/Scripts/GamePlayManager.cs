using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GamePlayManager : MonoBehaviour
{
    [Header("UI & Logic")]
    [SerializeField] private MeshRenderer targetObject;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private WindowBase pauseWindow;
    [SerializeField] private Slider volumeSlider;
    
    private int _currentScore = 0;

    private void Start()
    {
        LoadGameData();
        
        if (Inputs.Instance != null)
        {
            Inputs.Instance.EnableMap("Gameplay", 5);
            Inputs.Instance.EnableMap("Pausepressed", 10);
        }

        if (volumeSlider != null)
        {
            float savedVol = SaveSystem.playerData != null ? SaveSystem.playerData.masterVolume : 1f;
            volumeSlider.value = savedVol;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        _canCheckPause = false;
        if (pauseWindow != null) pauseWindow.gameObject.SetActive(false);
        Invoke(nameof(EnablePauseCheck), 0.1f);
    }

    private bool _canCheckPause = false;
    private void EnablePauseCheck() => _canCheckPause = true;

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        if (SaveSystem.playerData != null)
        {
            SaveSystem.playerData.masterVolume = volume;
        }
    }

    private void Update()
    {
        if (Inputs.Instance == null) return;
        
        if (_canCheckPause && Inputs.Instance.PausePressed)
        {
            if (pauseWindow != null && WindowManager.Instance != null && WindowManager.Instance.GetTopWindow() == null)
            {
                Debug.Log("[GamePlayManager] Pauză activată.");
                WindowManager.Instance.OpenWindow(pauseWindow);
                Time.timeScale = 0;
                return; 
            }
        }
        
        if (WindowManager.Instance != null && WindowManager.Instance.GetTopWindow() != null)
        {
            return;
        }
        
        bool actionTriggered = false;
        
        if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            actionTriggered = true;
        }
        else if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            actionTriggered = true;
        }
        
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (targetObject != null && (hit.collider.gameObject == targetObject.gameObject || hit.transform.IsChildOf(targetObject.transform)))
                    {
                        actionTriggered = true;
                    }
                }
            }
        }

        if (actionTriggered)
        {
            IncrementScore();
            ChangeObjectColor();
        }
    }

    private void IncrementScore()
    {
        _currentScore++;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {_currentScore}";
        }
    }

    private void ChangeObjectColor()
    {
        if (targetObject == null) return;
        
        Color newColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.5f, 1f);
        targetObject.material.color = newColor;
    }

    public void SaveGameData()
    {
        if (SaveSystem.playerData == null)
        {
            SaveSystem.playerData = new PlayerData();
        }

        SaveSystem.playerData.hasPlayData = true; 
        SaveSystem.playerData.score = _currentScore;
        SaveSystem.playerData.playerColor = targetObject != null ? targetObject.material.color : Color.white;

        SaveSystem.SaveData();
        Debug.Log("[GamePlayManager] Save.");
    }

    public void Unpause()
    {
        WindowManager.Instance.CloseTopWindow();
        Time.timeScale = 1;
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("WindowScene");
    }

    public void Settings(WindowBase newWindow)
    {
        WindowManager.Instance.OpenWindow(newWindow);
    }
    
    public void CloseWindow()
    {
        WindowManager.Instance.CloseTopWindow();
    }
    
    private void LoadGameData()
    {
        PlayerData data = SaveSystem.LoadData();
        
        if (data != null)
        {
            _currentScore = data.score;
            
            if (targetObject != null)
            {
                targetObject.material.color = data.playerColor;
            }

            AudioListener.volume = data.masterVolume;
        }

        UpdateScoreUI();
    }

    private void OnApplicationQuit()
    {
        SaveGameData();
    }
}

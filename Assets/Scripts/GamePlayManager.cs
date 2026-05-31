using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamePlayManager : MonoBehaviour
{
    [Header("UI & Logic")]
    [SerializeField] private MeshRenderer targetObject;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private WindowBase pauseWindow;
    
    private int _currentScore = 0;

    private void Start()
    {
        LoadGameData();
        
        if (Inputs.Instance != null)
        {
            Inputs.Instance.EnableMap("Gameplay", 5);
            Inputs.Instance.EnableMap("Pausepressed", 10);
        }

        _canCheckPause = false;
        if (pauseWindow != null) pauseWindow.gameObject.SetActive(false);
        Invoke(nameof(EnablePauseCheck), 0.1f);
    }

    private bool _canCheckPause = false;
    private void EnablePauseCheck() => _canCheckPause = true;

    private void Update()
    {
        if (Inputs.Instance == null) return;

        if (_canCheckPause && Inputs.Instance.PausePressed)
        {
            if (pauseWindow != null && WindowManager.Instance != null && WindowManager.Instance.GetTopWindow() == null)
            {
                Debug.Log("[GamePlayManager] Pauză activată.");
                WindowManager.Instance.OpenWindow(pauseWindow);
                return; 
            }
        }
        
        bool actionTriggered = false;
        
        // Verificăm butoanele generale (Enter, Space, Gamepad South)
        if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            actionTriggered = true;
        }
        else if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            actionTriggered = true;
        }
        
        // Verificăm Click-ul de Mouse cu Raycast
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
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

    public void Unpase()
    {
        WindowManager.Instance.CloseTopWindow();
        Time.timeScale = 1;
    }

    public void SaveGameData()
    {
        if (SaveSystem.playerData == null)
        {
            SaveSystem.playerData = new PlayerData();
        }

        SaveSystem.playerData.score = _currentScore;
        SaveSystem.playerData.playerColor = targetObject != null ? targetObject.material.color : Color.white;

        SaveSystem.SaveData();
        Debug.Log("[GamePlayManager] Save.");
    }

    private void LoadGameData()
    {
        // Apelăm LoadData din SaveSystem
        PlayerData data = SaveSystem.LoadData();
        
        if (data != null)
        {
            _currentScore = data.score;
            
            if (targetObject != null)
            {
                targetObject.material.color = data.playerColor;
            }

            // Aplicăm volumul salvat
            AudioListener.volume = data.masterVolume;
        }

        UpdateScoreUI();
        Debug.Log($"[GamePlayManager] Load Score: {_currentScore} | Volume: {AudioListener.volume}");
    }

    private void OnApplicationQuit()
    {
        SaveGameData();
    }
}

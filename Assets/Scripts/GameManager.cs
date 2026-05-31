using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private WindowBase newGameConfirmationWindow;
    [SerializeField] private Slider volumeSlider;

    private void Start()
    {
        PlayerData data = SaveSystem.LoadData();

        if (continueButton != null)
        {
            continueButton.interactable = SaveSystem.HasSave();
        }
        
        if (volumeSlider != null)
        {
            float savedVolume = data.masterVolume;
            volumeSlider.value = savedVolume;
            AudioListener.volume = savedVolume;
            
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        
        if (SaveSystem.playerData != null)
        {
            SaveSystem.playerData.masterVolume = volume;
        }
    }
public void ExitGame()
{
    SaveSystem.SaveData();

    Application.Quit();
    Debug.Log("Game Exited");

    #if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
    #endif
}

private void OnApplicationQuit()
{
    SaveSystem.SaveData();
}


    public void StartNewGame()
    {
        if (SaveSystem.HasSave())
        {
            if (newGameConfirmationWindow != null && WindowManager.Instance != null)
            {
                WindowManager.Instance.OpenWindow(newGameConfirmationWindow);
            }
            else
            {
                ConfirmStartNewGame(); 
            }
        }
        else
        {
            ConfirmStartNewGame();
        }
    }
    
    public void ConfirmStartNewGame()
    {
        SaveSystem.MakeNewSave();
        SceneManager.LoadScene("GamePlay");
    }

    public void LoadGame()
    {
        SaveSystem.LoadData();
        SceneManager.LoadScene("GamePlay");
    }

    public void SoundSave()
    {
        
    }
}

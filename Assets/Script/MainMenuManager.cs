using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[MainMenuManager] Start - MainMenu loaded.");
    }

    public void OnStartButton()
    {
        Debug.Log("[MainMenuManager] Start button pressed. Resetting session and loading GameplayFuture.");
        // Reset any previous session state
        GameSession.Reset();
        SceneManager.LoadScene(SceneNames.GameplayFuture);
    }

    // New: open Level selection scene
    public void OnLevelButton()
    {
        Debug.Log("[MainMenuManager] Level button pressed. Loading Levels scene.");
        SceneManager.LoadScene(SceneNames.Levels);
    }

    // Settings: load Settings scene
    public void OnSettingsButton()
    {
        Debug.Log("[MainMenuManager] Settings button pressed. Loading Settings scene.");
        SceneManager.LoadScene(SceneNames.Settings);
    }

    // Credits: load Credits scene
    public void OnCreditsButton()
    {
        Debug.Log("[MainMenuManager] Credits button pressed. Loading Credits scene.");
        SceneManager.LoadScene(SceneNames.Credits);
    }

    public void OnQuitButton()
    {
        Debug.Log("[MainMenuManager] Quit button pressed. Quitting application.");
        Application.Quit();
    }
}
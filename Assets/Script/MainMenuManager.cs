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

    public void OnSettingsButton()
    {
        Debug.Log("[MainMenuManager] Settings button pressed.");
        // TODO: show settings panel (implement UI panel in editor)
    }

    public void OnCreditsButton()
    {
        Debug.Log("[MainMenuManager] Credits button pressed.");
        // TODO: show credits panel
    }

    public void OnQuitButton()
    {
        Debug.Log("[MainMenuManager] Quit button pressed. Quitting application.");
        Application.Quit();
    }
}
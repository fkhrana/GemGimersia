using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Small helper for buttons inside overlay scenes (Levels/Settings/Credits).
/// BackToMainMenu() will close additive overlay + resume game if overlay was opened
/// from pause; otherwise it will load the MainMenu scene.
/// </summary>
public class SceneNavigation : MonoBehaviour
{
    /// <summary>
    /// Called by Back button in Levels/Settings/Credits.
    /// If an overlay was loaded via OverlaySceneLoader, close it and resume the paused game.
    /// Otherwise load the MainMenu scene.
    /// </summary>
    public void BackToMainMenu()
    {
        // If an overlay is currently managed by OverlaySceneLoader, close it and resume the game
        if (OverlaySceneLoader.Instance != null && !string.IsNullOrEmpty(OverlaySceneLoader.Instance.currentOverlayScene))
        {
            Debug.Log("[SceneNavigation] Back pressed inside overlay - closing overlay and resuming game.");
            OverlaySceneLoader.Instance.CloseOverlayAndResume();
            return;
        }

        // Fallback: no overlay -> go back to MainMenu
        Debug.Log("[SceneNavigation] Back pressed (no overlay) - loading MainMenu.");
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    /// <summary>
    /// Generic loader helper (optional): useful if you want a button inside an overlay to open another scene.
    /// </summary>
    /// <param name="sceneName"></param>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneNavigation] LoadScene called with empty name.");
            return;
        }
        Debug.Log($"[SceneNavigation] Loading scene '{sceneName}'.");
        SceneManager.LoadScene(sceneName);
    }
}
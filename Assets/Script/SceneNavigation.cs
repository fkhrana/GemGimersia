using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Small helper for scene navigation. Attach to a GameObject (e.g. UI root) in Levels/Settings/Credits.
/// - BackToMainMenu() : load MainMenu
/// - LoadScene(string) : load any scene by name (useful for wiring buttons)
/// </summary>
public class SceneNavigation : MonoBehaviour
{
    /// <summary>
    /// Load the Main Menu scene (SceneNames.MainMenu)
    /// </summary>
    public void BackToMainMenu()
    {
        Debug.Log("[SceneNavigation] BackToMainMenu pressed. Loading MainMenu.");
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    /// <summary>
    /// Load any scene by name. Useful if you want one script for many buttons.
    /// Set the scene name string in the Button OnClick parameter.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load (must be added to Build Settings)</param>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneNavigation] LoadScene called with empty sceneName.");
            return;
        }
        Debug.Log($"[SceneNavigation] Loading scene '{sceneName}'.");
        SceneManager.LoadScene(sceneName);
    }
}
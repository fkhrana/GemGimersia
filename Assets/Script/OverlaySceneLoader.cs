using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Load/unload small UI scenes as additive overlays (Levels / Settings / Credits).
/// Keep the gameplay scene loaded and paused while overlays are open.
/// Use LoadOverlay(sceneName) from the Pause menu; Back button should call
/// SceneNavigation.BackToMainMenu() which will close the overlay and resume the game.
/// </summary>
public class OverlaySceneLoader : MonoBehaviour
{
    public static OverlaySceneLoader Instance { get; private set; }

    // name of currently loaded overlay scene (null if none)
    public string currentOverlayScene { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // keep alive so it can manage overlays while gameplay scene is loaded/unloaded
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Public entrypoint for UI buttons (can be wired in Inspector and accept a string parameter).
    /// </summary>
    /// <param name="sceneName">Scene name (must be in Build Settings)</param>
    public void LoadOverlay(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[OverlaySceneLoader] LoadOverlay called with empty sceneName.");
            return;
        }
        StartCoroutine(LoadOverlayCoroutine(sceneName));
    }

    IEnumerator LoadOverlayCoroutine(string sceneName)
    {
        // if same overlay already open, do nothing
        if (currentOverlayScene == sceneName)
        {
            Debug.Log($"[OverlaySceneLoader] Overlay '{sceneName}' already loaded.");
            yield break;
        }

        // if another overlay is open, unload it first
        if (!string.IsNullOrEmpty(currentOverlayScene))
        {
            yield return SceneManager.UnloadSceneAsync(currentOverlayScene);
            currentOverlayScene = null;
            yield return null;
        }

        Debug.Log($"[OverlaySceneLoader] Loading overlay scene '{sceneName}' additively...");
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone)
            yield return null;

        currentOverlayScene = sceneName;
        Debug.Log($"[OverlaySceneLoader] Overlay '{sceneName}' loaded.");
    }

    /// <summary>
    /// Close overlay (if any) and resume the game using UIManager.OnContinueFromPause().
    /// Use this from Back button in overlay scenes.
    /// </summary>
    public void CloseOverlayAndResume()
    {
        StartCoroutine(CloseAndResumeCoroutine());
    }

    IEnumerator CloseAndResumeCoroutine()
    {
        if (string.IsNullOrEmpty(currentOverlayScene))
        {
            Debug.Log("[OverlaySceneLoader] No overlay to close - calling resume anyway.");
            // Still call resume in case game was paused and user expects resume
            if (UIManager.Instance != null)
                UIManager.Instance.OnContinueFromPause();
            yield break;
        }

        string closing = currentOverlayScene;
        Debug.Log($"[OverlaySceneLoader] Unloading overlay '{closing}'...");
        var op = SceneManager.UnloadSceneAsync(closing);
        while (op != null && !op.isDone)
            yield return null;

        currentOverlayScene = null;
        // small frame delay to ensure scene unload completed
        yield return null;

        Debug.Log($"[OverlaySceneLoader] Overlay '{closing}' unloaded. Resuming game...");
        if (UIManager.Instance != null)
            UIManager.Instance.OnContinueFromPause();
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Load/unload small UI scenes as additive overlays (Levels / Settings / Credits).
/// Keep the gameplay scene loaded and paused while overlays are open.
/// When an overlay is opened from the Pause popup we hide the pause panel and disable pause inputs so they
/// don't block the overlay UI. When closing, we re-enable pause inputs and resume.
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

        // HIDE the Pause panel if the game is paused so it won't block overlay UI.
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.pausePanel != null && UIManager.Instance.isPaused)
            {
                UIManager.Instance.pausePanel.SetActive(false);
                Debug.Log("[OverlaySceneLoader] Hid UIManager.pausePanel so overlay is usable.");
            }

            // Disable pause-related inputs/buttons (so pause can't be re-opened while overlay is active)
            UIManager.Instance.SetPauseInputsEnabled(false);
        }

        // Clear any current EventSystem selection so overlay buttons won't be blocked
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
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
            {
                // re-enable pause inputs just in case
                UIManager.Instance.SetPauseInputsEnabled(true);
                UIManager.Instance.OnContinueFromPause();
            }
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

        // Re-enable pause-related inputs/buttons before resume
        if (UIManager.Instance != null)
            UIManager.Instance.SetPauseInputsEnabled(true);

        if (UIManager.Instance != null)
            UIManager.Instance.OnContinueFromPause();
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD (TextMeshPro)")]
    public TextMeshProUGUI startText;
    public TextMeshProUGUI infoText;

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject winPanel;

    [Header("Countdown (TextMeshPro)")]
    public TextMeshProUGUI countdownText; // big overlay text for 3..2..1

    [HideInInspector]
    public bool isPaused = false;

    // soft-fall tuning (exposed so you can tweak in inspector)
    [Header("Resume Soft-Fall (optional)")]
    [Tooltip("Duration in seconds (real time) to apply reduced gravity just after resume.")]
    public float resumeSoftFallDuration = 0.25f;
    [Tooltip("Multiplier applied to player's gravityScale while soft-fall (0.0 - 1.0 typical).")]
    public float resumeSoftFallGravityMultiplier = 0.45f;
    [Tooltip("Downward nudge applied to airborne player to ensure falling begins (set to 0 to disable).")]
    public float resumeDownwardNudge = -0.15f;

    [Header("Pause input controls")]
    [Tooltip("Assign any Buttons that should be disabled while an overlay is open (eg. Pause icon, Pause panel buttons).")]
    public Button[] pauseButtons;

    [Header("Win input controls")]
    [Tooltip("Assign any Buttons on the Win popup that should be disabled while an overlay is open.")]
    public Button[] winButtons;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[UIManager] Awake - instance created.");
        }
        else
        {
            Debug.LogWarning("[UIManager] Awake - duplicate instance detected, destroying this one.");
            Destroy(gameObject);
        }
    }

    #region Start/Info
    public void ShowStartText(string s)
    {
        if (startText != null)
        {
            startText.gameObject.SetActive(true);
            startText.text = s;
            Debug.Log($"[UIManager] ShowStartText: '{s}'");
        }
    }

    public void HideStartText()
    {
        if (startText != null)
        {
            startText.gameObject.SetActive(false);
            Debug.Log("[UIManager] HideStartText called.");
        }
    }

    public void ShowInfoText(string s)
    {
        if (infoText != null)
        {
            infoText.gameObject.SetActive(true);
            infoText.text = s;
            Debug.Log($"[UIManager] ShowInfoText: '{s}'");
        }
    }

    public void HideInfoText()
    {
        if (infoText != null)
        {
            infoText.gameObject.SetActive(false);
            Debug.Log("[UIManager] HideInfoText called.");
        }
    }
    #endregion

    #region Pause
    public void OnPauseButton()
    {
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        isPaused = true;
        Debug.Log("[UIManager] OnPauseButton - game paused and pausePanel shown.");
    }

    public void OnBackToMenuFromPause()
    {
        Time.timeScale = 1f;
        isPaused = false;
        Debug.Log("[UIManager] OnBackToMenuFromPause - returning to MainMenu.");
        GameSession.Reset();
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    public void OnContinueFromPause()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        Debug.Log("[UIManager] OnContinueFromPause - hiding pausePanel and starting countdown.");

        // Clear any pending inputs that might have been captured during pause
        var playerForClear = (GameManager.Instance != null) ? GameManager.Instance.player : null;
        if (playerForClear != null)
            playerForClear.ClearPendingInput();

        StartCoroutine(ResumeWithCountdown());
    }

    IEnumerator ResumeWithCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            for (int i = 3; i >= 1; i--)
            {
                countdownText.text = i.ToString();
                Debug.Log($"[UIManager] Countdown: {i}");
                // use realtime wait so countdown runs while timescale=0
                yield return new WaitForSecondsRealtime(1f);
            }
            countdownText.gameObject.SetActive(false);
        }

        // Clear UI selection to avoid click propagation
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        // Wait a frame for UI state to settle
        yield return null;

        // Wait until mouse button and Space are released to avoid click/hold-through.
        yield return new WaitUntil(() => !Input.GetMouseButton(0) && !Input.GetKey(KeyCode.Space));

        // BEFORE resuming physics, prepare player so effects take place immediately when physics restarts
        var player = (GameManager.Instance != null) ? GameManager.Instance.player : null;
        if (player != null)
        {
            // discard any pending queued input (prevents queued jump)
            player.ClearPendingInput();

            // cancel upward momentum immediately (so no auto jump)
            player.ForceDropVertical();

            // brief ignore window for any accidental immediate presses (unscaled)
            player.IgnoreInputForSeconds(0.12f);

            // give a small downward nudge if player is airborne so SoftFall is visible
            if (resumeDownwardNudge != 0f)
                player.NudgeDownIfAirborne(resumeDownwardNudge);

            // apply soft-fall BEFORE physics resumes so reduced gravity is in effect when physics restarts
            player.SoftFallForSeconds(resumeSoftFallDuration, resumeSoftFallGravityMultiplier);

            Debug.Log("[UIManager] Player prepared for resume: ForceDrop + IgnoreInput + Nudge + SoftFall applied (before timescale=1).");
        }

        // Now resume: clear pause flag then restore timescale
        isPaused = false;
        Time.timeScale = 1f;
        Debug.Log("[UIManager] Countdown finished and inputs released. Game resumed (Time.timeScale = 1).");
    }
    #endregion

    #region Pause & Win input helpers
    /// <summary>
    /// Enable or disable assigned pause-related Buttons (useful while overlays are open).
    /// Assign in Inspector all Buttons that should be disabled while an overlay is open.
    /// </summary>
    public void SetPauseInputsEnabled(bool enabled)
    {
        if (pauseButtons == null || pauseButtons.Length == 0) return;
        for (int i = 0; i < pauseButtons.Length; i++)
        {
            if (pauseButtons[i] != null)
                pauseButtons[i].interactable = enabled;
        }
        Debug.Log($"[UIManager] SetPauseInputsEnabled({enabled}) applied to {pauseButtons.Length} buttons.");
    }

    /// <summary>
    /// Enable or disable assigned win-related Buttons (useful while overlays are open).
    /// </summary>
    public void SetWinInputsEnabled(bool enabled)
    {
        if (winButtons == null || winButtons.Length == 0) return;
        for (int i = 0; i < winButtons.Length; i++)
        {
            if (winButtons[i] != null)
                winButtons[i].interactable = enabled;
        }
        Debug.Log($"[UIManager] SetWinInputsEnabled({enabled}) applied to {winButtons.Length} buttons.");
    }
    #endregion

    #region Overlay helper (for Pause/Win menu buttons)
    /// <summary>
    /// Open an overlay scene (Levels / Settings / Credits) via OverlaySceneLoader.
    /// Use this for Pause or Win menu buttons instead of wiring directly to OverlaySceneLoader,
    /// so the OnClick target remains valid across scene reloads.
    /// </summary>
    /// <param name="sceneName">Name of the overlay scene (must be in Build Settings)</param>
    public void OpenOverlay(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[UIManager] OpenOverlay called with empty sceneName.");
            return;
        }

        // Ensure OverlaySceneLoader exists (create if missing). OverlaySceneLoader is DontDestroyOnLoad.
        if (OverlaySceneLoader.Instance == null)
        {
            var go = new GameObject("OverlaySceneLoader");
            go.AddComponent<OverlaySceneLoader>();
            Debug.Log("[UIManager] Created OverlaySceneLoader instance at runtime.");
        }

        // Hide pausePanel right away to avoid blocking overlay UI (OverlaySceneLoader will also hide it after load)
        if (pausePanel != null && isPaused)
            pausePanel.SetActive(false);

        // Hide winPanel right away if it's active (when called from Win popup)
        if (winPanel != null && winPanel.activeSelf)
            winPanel.SetActive(false);

        // Disable pause-related inputs so they can't be used while overlay is open
        SetPauseInputsEnabled(false);
        // Disable win-related inputs as well
        SetWinInputsEnabled(false);

        OverlaySceneLoader.Instance.LoadOverlay(sceneName);
    }
    #endregion

    #region Win / other UI...
    public void ShowWinPopup()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Debug.Log("[UIManager] ShowWinPopup - Win panel shown.");
        }
    }

    public void OnBackToMenuFromWin()
    {
        Time.timeScale = 1f; // ensure timescale normal
        isPaused = false;
        Debug.Log("[UIManager] OnBackToMenuFromWin - returning to MainMenu and resetting session.");
        GameSession.Reset();
        SceneManager.LoadScene(SceneNames.MainMenu);
    }
    #endregion
}
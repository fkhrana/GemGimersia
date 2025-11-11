using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

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
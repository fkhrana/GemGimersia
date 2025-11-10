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

    // public flag used by PlayerController to detect UI pause state
    [HideInInspector]
    public bool isPaused = false;

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

        // Resume: clear pause flag then restore timescale
        isPaused = false;
        Time.timeScale = 1f;
        Debug.Log("[UIManager] Countdown finished and inputs released. Game resumed (Time.timeScale = 1).");

        // Small extra safety window to ignore accidental immediate presses
        var player = (GameManager.Instance != null) ? GameManager.Instance.player : null;
        if (player != null)
            player.IgnoreInputForSeconds(0.12f);
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
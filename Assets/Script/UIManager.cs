using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

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
        // Pause game
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        Debug.Log("[UIManager] OnPauseButton - game paused and pausePanel shown.");
    }

    public void OnBackToMenuFromPause()
    {
        // Unpause time scale to load menu cleanly
        Time.timeScale = 1f;
        Debug.Log("[UIManager] OnBackToMenuFromPause - returning to MainMenu.");
        GameSession.Reset();
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    public void OnContinueFromPause()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        Debug.Log("[UIManager] OnContinueFromPause - hiding pausePanel and starting countdown.");
        // Start countdown and resume using realtime wait
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
                yield return new WaitForSecondsRealtime(1f);
            }
            countdownText.gameObject.SetActive(false);
        }
        // Resume game
        Time.timeScale = 1f;
        Debug.Log("[UIManager] Countdown finished. Game resumed (Time.timeScale = 1).");
    }
    #endregion

    #region Win
    public void ShowWinPopup()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Debug.Log("[UIManager] ShowWinPopup - Win panel shown.");
        }
    }

    // Called by Win panel button
    public void OnBackToMenuFromWin()
    {
        Time.timeScale = 1f; // ensure timescale normal
        Debug.Log("[UIManager] OnBackToMenuFromWin - returning to MainMenu and resetting session.");
        GameSession.Reset();
        SceneManager.LoadScene(SceneNames.MainMenu);
    }
    #endregion
}
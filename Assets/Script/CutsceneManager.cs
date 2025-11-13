using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour
{
    [Header("Cutscene Frames")]
    public Image displayImage;
    public Sprite[] cutsceneFrames;

    [Header("Settings")]
    public float fadeDuration = 0.5f;

    int currentFrame = 0;
    bool playing = true;
    bool inputLocked = false;

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetGameplayActive(false); // 🚫 disable gameplay

        if (GameSession.hasPlayedIntro)
        {
            Debug.Log("[CutsceneManager] Cutscene already played, skipping.");
            EndCutscene();
            return;
        }

        GameSession.hasPlayedIntro = true;

        if (cutsceneFrames.Length == 0 || displayImage == null)
        {
            Debug.LogWarning("[CutsceneManager] Missing frames or display image, skipping cutscene.");
            EndCutscene();
            return;
        }

        displayImage.sprite = cutsceneFrames[currentFrame];
        Color c = displayImage.color;
        c.a = 1f;
        displayImage.color = c;
    }

    void Update()
    {
        if (!playing || inputLocked) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            StartCoroutine(NextFrame());
    }

    IEnumerator NextFrame()
    {
        inputLocked = true;

        currentFrame++;
        if (currentFrame >= cutsceneFrames.Length)
        {
            EndCutscene();
            yield break;
        }

        yield return StartCoroutine(FadeToFrame(cutsceneFrames[currentFrame]));
        inputLocked = false;
    }

    IEnumerator FadeToFrame(Sprite next)
    {
        float t = 0f;
        Color c = displayImage.color;

        // Fade out
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = 1f - (t / fadeDuration);
            displayImage.color = c;
            yield return null;
        }

        displayImage.sprite = next;
        t = 0f;

        // Fade in
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = t / fadeDuration;
            displayImage.color = c;
            yield return null;
        }
    }

    void EndCutscene()
    {
        playing = false;
        Debug.Log("[CutsceneManager] Cutscene ended. Enabling player control.");

        // ✅ Reactivate gameplay after cutscene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameplayActive(true);
            GameManager.Instance.StartGameplay();
        }

        if (displayImage != null)
            displayImage.gameObject.SetActive(false);

        gameObject.SetActive(true);
    }
}

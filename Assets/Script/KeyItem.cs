using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class KeyItem : MonoBehaviour
{
    public ParticleSystem pickupEffect;
    public AudioClip pickupSfx;
    AudioSource audioSource;

    void Awake()
    {
        if (pickupSfx != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = pickupSfx;
        }
        Debug.Log("[KeyItem] Awake - Key ready.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[KeyItem] Player triggered Key. Recording position, playing effect, and loading GameplayPast.");

        // Record key pickup position and mark session
        GameSession.hasKey = true;
        GameSession.lastKeyPosition = other.transform.position;
        Debug.Log($"[KeyItem] lastKeyPosition = {GameSession.lastKeyPosition}");

        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        if (audioSource != null)
            audioSource.Play();

        // Disable visuals
        gameObject.SetActive(false);

        // Load Past scene
        SceneManager.LoadScene(SceneNames.GameplayPast);
    }
}
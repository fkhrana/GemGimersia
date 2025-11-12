using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class KeyItem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public ParticleSystem pickupEffect;
    public AudioClip pickupSfx;
    public float pickupDelay = 1.0f; // Delay before loading next scene

    AudioSource audioSource;
    bool pickedUp = false;

    void Awake()
    {
        // Setup audio source if a sound is assigned
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
        if (pickedUp || !other.CompareTag("Player"))
            return;

        pickedUp = true;
        Debug.Log("[KeyItem] Player triggered Key. Playing effects and scheduling scene load.");

        // Record pickup in session
        GameSession.hasKey = true;
        GameSession.lastKeyPosition = other.transform.position;
        Debug.Log($"[KeyItem] lastKeyPosition = {GameSession.lastKeyPosition}");

        // Spawn visual effect
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        // Play sound
        if (audioSource != null)
            audioSource.Play();

        // Hide key immediately (so it looks picked up)
        GetComponent<Collider2D>().enabled = false;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = false;

        // Delay before scene transition
        Invoke(nameof(LoadNextScene), pickupDelay);
    }

    void LoadNextScene()
    {
        Debug.Log("[KeyItem] Loading GameplayPast scene after delay.");
        SceneManager.LoadScene(SceneNames.GameplayPast);
    }
}

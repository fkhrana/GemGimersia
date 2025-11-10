using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MachineTrigger : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("[MachineTrigger] Awake - Machine trigger ready.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log("[MachineTrigger] Player entered machine trigger. Checking key state.");
        if (GameSession.hasKey)
        {
            Debug.Log("[MachineTrigger] Player has key - notifying GameManager for win.");
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerReachedMachine();
        }
        else
        {
            Debug.Log("[MachineTrigger] Player does not have key - ignoring.");
        }
    }
}
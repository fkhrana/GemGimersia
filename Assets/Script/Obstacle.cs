using UnityEngine;

/// <summary>
/// Obstacle: attach to obstacle objects (requires Collider2D).
/// On collision/trigger with Player, either call GameManager.OnPlayerDied()
/// (reload scene / game over) or teleport player back to GameManager.startPoint.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Obstacle : MonoBehaviour
{
    [Tooltip("If true, call GameManager.OnPlayerDied() (scene reload). If false, teleport player back to GameManager.startPoint.")]
    public bool reloadSceneOnHit = true;

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.collider);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other);
    }

    void HandleCollision(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[Obstacle] GameManager.Instance is null - no action taken on player hit.");
            return;
        }

        if (reloadSceneOnHit)
        {
            Debug.Log("[Obstacle] Player hit obstacle - calling GameManager.OnPlayerDied().");
            gm.OnPlayerDied();
        }
        else
        {
            Debug.Log("[Obstacle] Player hit obstacle - teleporting to StartPoint (no scene reload).");
            var player = gm.player;
            if (player != null)
            {
                // teleport to startPoint if assigned, otherwise to Vector3.zero
                if (gm.startPoint != null)
                    player.transform.position = gm.startPoint.position;
                else
                    player.transform.position = Vector3.zero;

                // wake up rb and clear velocity
                player.rbWakeUp();
                if (player.rb != null)
                    player.rb.linearVelocity = Vector2.zero;

                // mark as not started and show start prompt
                player.started = false;
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowStartText("Press Space or Left Click to start");
            }
            else
            {
                Debug.LogWarning("[Obstacle] GameManager.player is null - cannot teleport player.");
            }
        }
    }
}
using UnityEngine;

// PlayerController: input = Space or Left Mouse Button
// Uses Rigidbody2D.linearVelocity as requested.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;       // horizontal speed
    public float jumpForce = 6f;       // vertical impulse applied on input
    public float maxY = 4.5f;          // ceiling clamp
    public float minY = -4.5f;         // floor clamp

    [Header("State")]
    public bool started = false;
    public int direction = -1;         // -1 = left, +1 = right

    [HideInInspector]
    public Rigidbody2D rb;
    SpriteRenderer sr;
    GameManager gm;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        // don't cache GameManager here — Awake order between objects is not deterministic
        Debug.Log("[PlayerController] Awake - components cached.");
    }

    void Start()
    {
        // Freeze horizontal movement until started
        rb.linearVelocity = Vector2.zero;

        // Safe place to cache GameManager.Instance because all Awake() calls have run
        gm = GameManager.Instance;
        if (gm == null)
            Debug.LogWarning("[PlayerController] Start - GameManager.Instance is null. OnPlayerDied calls will fallback to GameManager.Instance when needed.");
        else
            Debug.Log("[PlayerController] Start - GameManager instance cached.");
    }

    void Update()
    {
        // Input only Space or left mouse
        if (!started)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                started = true;
                rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
                Debug.Log($"[PlayerController] Game started by input. Direction={direction}, horizontal velocity set to {rb.linearVelocity.x}.");
                if (UIManager.Instance != null) UIManager.Instance.HideStartText();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                // Reset vertical velocity then apply jump impulse
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                Debug.Log("[PlayerController] Input detected - Flap applied (jumpForce).");
            }

            // Keep horizontal velocity steady
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }

        // Clamp vertical position
        Vector3 pos = transform.position;
        float clampedY = Mathf.Clamp(pos.y, minY, maxY);
        if (clampedY != pos.y)
        {
            Debug.Log($"[PlayerController] Position clamped from y={pos.y} to y={clampedY}.");
        }
        pos.y = clampedY;
        transform.position = pos;
    }

    public void SetDirection(int newDir)
    {
        direction = Mathf.Clamp(newDir, -1, 1);
        if (sr != null) sr.flipX = direction > 0 ? true : false;
        if (started)
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
            Debug.Log($"[PlayerController] SetDirection called. New direction={direction}, updated linearVelocity.x={rb.linearVelocity.x}");
        }
        else
        {
            Debug.Log($"[PlayerController] SetDirection called. New direction={direction}, player not started yet.");
        }
    }

    // Helper to ensure physics awake when changing scene
    public void rbWakeUp()
    {
        if (rb != null)
        {
            rb.WakeUp();
            Debug.Log("[PlayerController] rbWakeUp called - Rigidbody woke up.");
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // If hit obstacle, ground, or ceiling => immediate restart to GameplayFuture
        if (collision.collider.CompareTag("Obstacle") ||
            collision.collider.CompareTag("Ground") ||
            collision.collider.CompareTag("Ceiling"))
        {
            Debug.Log($"[PlayerController] OnCollisionEnter2D with '{collision.collider.tag}'. Triggering OnPlayerDied.");

            // fallback: in case gm wasn't cached in Start(), re-resolve here
            if (gm == null)
                gm = GameManager.Instance;

            if (gm != null)
            {
                gm.OnPlayerDied();
            }
            else
            {
                Debug.LogError("[PlayerController] OnCollisionEnter2D - GameManager.Instance is null, cannot call OnPlayerDied().");
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Note: Key pickup handled by KeyItem to avoid duplicate scene loads
        if (other.CompareTag("Key"))
        {
            Debug.Log("[PlayerController] OnTriggerEnter2D detected Key trigger. (Key handling is in KeyItem)");
        }
    }
}
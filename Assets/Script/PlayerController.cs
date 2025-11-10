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

    // --- ignore input after resume (unscaled time)
    float ignoreInputUntil = 0f;
    // require release of held inputs before accepting new input
    bool blockUntilRelease = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        Debug.Log("[PlayerController] Awake - components cached.");
    }

    void Start()
    {
        rb.linearVelocity = Vector2.zero;
        gm = GameManager.Instance;
        if (gm == null)
            Debug.LogWarning("[PlayerController] Start - GameManager.Instance is null.");
        else
            Debug.Log("[PlayerController] Start - GameManager instance cached.");
    }

    void Update()
    {
        // detect held state
        bool isMouseHeld = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
        bool isSpaceHeld = Input.GetKey(KeyCode.Space);

        // Determine whether to skip input:
        bool timeBlock = Time.unscaledTime < ignoreInputUntil;
        bool holdBlock = blockUntilRelease && (isMouseHeld || isSpaceHeld);
        bool skipInput = timeBlock || holdBlock;

        // If blockUntilRelease was true and no buttons are held and timeout passed, clear the block.
        if (blockUntilRelease && !isMouseHeld && !isSpaceHeld && !timeBlock)
        {
            blockUntilRelease = false;
            Debug.Log("[PlayerController] blockUntilRelease cleared - inputs released and timeout passed.");
        }

        // Input only Space or left mouse
        if (!started)
        {
            if (!skipInput && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            {
                started = true;
                rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
                Debug.Log($"[PlayerController] Game started by input. Direction={direction}, horizontal velocity set to {rb.linearVelocity.x}.");
                if (UIManager.Instance != null) UIManager.Instance.HideStartText();
            }
        }
        else
        {
            if (!skipInput && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            {
                // Reset vertical velocity then apply jump impulse
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                Debug.Log("[PlayerController] Input detected - Flap applied (jumpForce).");
            }

            // Always enforce horizontal velocity while started so player keeps moving even if skipInput is active.
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }

        // Clamp vertical position
        Vector3 pos = transform.position;
        float clampedY = Mathf.Clamp(pos.y, minY, maxY);
        if (clampedY != pos.y)
            Debug.Log($"[PlayerController] Position clamped from y={pos.y} to y={clampedY}.");
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
        if (collision.collider.CompareTag("Obstacle") ||
            collision.collider.CompareTag("Ground") ||
            collision.collider.CompareTag("Ceiling"))
        {
            Debug.Log($"[PlayerController] OnCollisionEnter2D with '{collision.collider.tag}'. Triggering OnPlayerDied.");
            if (gm == null) gm = GameManager.Instance;
            if (gm != null) gm.OnPlayerDied();
            else Debug.LogError("[PlayerController] OnCollisionEnter2D - GameManager.Instance is null.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Key"))
            Debug.Log("[PlayerController] OnTriggerEnter2D detected Key trigger. (Key handling is in KeyItem)");
    }

    // public API: ignore input for a short unscaled time window and require release of held inputs
    // Call from UIManager after countdown. Keep Time.timeScale controlled in UIManager coroutine.
    public void IgnoreInputForSeconds(float seconds)
    {
        ignoreInputUntil = Time.unscaledTime + Mathf.Max(0f, seconds);
        blockUntilRelease = true;
        Debug.Log($"[PlayerController] Ignoring input until unscaled time {ignoreInputUntil:F2} (for {seconds:F2}s). blockUntilRelease={blockUntilRelease}");
    }
}